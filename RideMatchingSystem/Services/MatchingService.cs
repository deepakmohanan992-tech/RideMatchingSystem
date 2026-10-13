using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Services
{
    public class MatchingService
    {
        private readonly DriverService _driverService;
        private readonly RideService _rideService;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(DriverService driverService, RideService rideService, ILogger<MatchingService> logger)
        {
            _driverService = driverService;
            _rideService = rideService;
            _logger = logger;
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

            // Try to assign driver to ride via RideService which handles concurrency/transactions
            var success = await _rideService.TryAssignDriverAsync(ride.Id, driver.Id, ct);
            if (success)
            {
                _logger.LogInformation("Assigned driver {DriverId} to ride {RideId}", driver.Id, ride.Id);
            }
            else
            {
                _logger.LogWarning("Failed to assign driver {DriverId} to ride {RideId} (concurrency or unavailable)", driver.Id, ride.Id);
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

