using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RideMatchingSystem.Models;
using Microsoft.AspNetCore.SignalR;
using RideMatchingSystem.Hubs;
using RideMatchingSystem.DTOs;

namespace RideMatchingSystem.Services
{
    public class MatchingService
    {
        private readonly DriverService _driverService;
        private readonly RideService _rideService;
        private readonly ILogger<MatchingService> _logger;
        private readonly IHubContext<RiderHub, IRiderClient> _riderHub;
        private readonly IHubContext<DriverHub, IDriverClient> _driverHub;
        private readonly IConnectionRegistry _registry;
        private readonly IOfferStore _offerStore;

        public MatchingService(DriverService driverService, RideService rideService, ILogger<MatchingService> logger, IHubContext<RiderHub, IRiderClient> riderHub, IHubContext<DriverHub, IDriverClient> driverHub, IConnectionRegistry registry, IOfferStore offerStore)
        {
            _driverService = driverService;
            _rideService = rideService;
            _logger = logger;
            _riderHub = riderHub;
            _driverHub = driverHub;
            _registry = registry;
            _offerStore = offerStore;
        }

        // Haversine distance in meters
        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth radius in meters
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double deg) => deg * (Math.PI / 180);

        public async Task<Driver?> FindNearestDriverForRideAsync(Ride ride, CancellationToken ct = default)
        {
            if (ride == null) return null;

            var drivers = await _driverService.GetAvailableDriversAsync(ct);
            if (drivers == null) return null;

            var now = DateTime.UtcNow;

            var candidates = drivers
                .Where(d => d.Latitude.HasValue && d.Longitude.HasValue && (now - d.LastSeenUtc).TotalMinutes <= 5)
                .Select(d => new { Driver = d, Distance = HaversineDistance(ride.PickupLatitude, ride.PickupLongitude, d.Latitude!.Value, d.Longitude!.Value) })
                .OrderBy(x => x.Distance)
                .ToList();

            var nearest = candidates.FirstOrDefault();
            if (nearest == null)
            {
                _logger.LogInformation("No available drivers found near ride {RideId}", ride.Id);
                return null;
            }

            _logger.LogInformation("Nearest driver for ride {RideId} is {DriverId} at distance {Distance} meters", ride.Id, nearest.Driver.Id, nearest.Distance);
            return nearest.Driver;
        }

        public async Task<bool> TryMatchRideAsync(Ride ride, CancellationToken ct = default)
        {
            var driver = await FindNearestDriverForRideAsync(ride, ct);
            if (driver == null) return false;

            // Create an ephemeral offer and notify the driver(s). Actual assignment happens when driver accepts.
            var offerId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddSeconds(30);

            var offerRecord = new OfferInfo(offerId, ride.Id, driver.Id, expiresAt);
            _offerStore.Add(offerRecord);

            // Build offer DTO using existing record shape
            var offerDto = new RideMatchOfferDto(offerId, ride.Id.ToString(), driver.Id, 0.0, new LocationDto { Latitude = driver.Latitude ?? 0, Longitude = driver.Longitude ?? 0 }, 0m, expiresAt);

            try
            {
                // Notify driver via registry if we have connections
                var conns = _registry.GetConnections(driver.Id).ToList();
                if (conns.Any())
                {
                    foreach (var cid in conns)
                    {
                        await _driverHub.Clients.Client(cid).ReceiveRideOffer(offerDto);
                    }
                }
                else
                {
                    // Fallback: broadcast to all drivers (demo)
                    await _driverHub.Clients.All.ReceiveRideOffer(offerDto);
                }

                // Notify rider that an offer was created (they may be subscribed to ride group)
                var rideDto = new RideResponseDto(ride.Id.ToString(), ride.RiderId, Models.RideStatus.Requested.ToString(), new LocationDto { Latitude = ride.PickupLatitude, Longitude = ride.PickupLongitude }, null, ride.RequestedAtUtc, 1);
                await _riderHub.Clients.Group($"ride:{ride.Id}").RideStatusUpdated(rideDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending offer notifications for ride {RideId}", ride.Id);
            }

            return true;
        }

        public async Task<bool> AcceptOfferAsync(string offerId, int driverId, CancellationToken ct = default)
        {
            if (!_offerStore.TryGet(offerId, out var offer))
                return false;

            if (offer.DriverId != driverId)
                return false;

            // Try to assign driver to ride via RideService which handles concurrency/transactions
            var success = await _rideService.TryAssignDriverAsync(offer.RideId, driverId, ct);
            if (success)
            {
                _logger.LogInformation("Driver {DriverId} accepted offer {OfferId} and was assigned to ride {RideId}", driverId, offerId, offer.RideId);

                // Notify rider group
                var ride = await _rideService.GetRideByIdAsync(offer.RideId, ct);
                if (ride != null)
                {
                    var rideDto = new RideResponseDto(ride.Id.ToString(), ride.RiderId, Models.RideStatus.Assigned.ToString(), new LocationDto { Latitude = ride.PickupLatitude, Longitude = ride.PickupLongitude }, null, ride.RequestedAtUtc, 1);
                    await _riderHub.Clients.Group($"ride:{ride.Id}").RideStatusUpdated(rideDto);

                    // Notify driver connections that assignment succeeded
                    var conns = _registry.GetConnections(driverId).ToList();
                    foreach (var cid in conns)
                    {
                        await _driverHub.Clients.Client(cid).RideAssigned(rideDto);
                    }
                }

                _offerStore.Remove(offerId);
            }
            else
            {
                _logger.LogWarning("Failed to assign driver {DriverId} for offer {OfferId} (concurrency or unavailable)", driverId, offerId);
                // inform driver that offer is no longer valid
                var conns = _registry.GetConnections(driverId).ToList();
                foreach (var cid in conns)
                {
                    await _driverHub.Clients.Client(cid).OfferCancelled(offerId);
                }
                _offerStore.Remove(offerId);
            }

            return success;
        }

        public async Task<int> MatchPendingRidesAsync(CancellationToken ct = default)
        {
            var pending = await _rideService.GetPendingRidesAsync(ct);
            var matched = 0;

            foreach (var ride in pending)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var ok = await TryMatchRideAsync(ride, ct);
                    if (ok) matched++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error matching ride {RideId}", ride.Id);
                }
            }

            return matched;
        }
    }
}

