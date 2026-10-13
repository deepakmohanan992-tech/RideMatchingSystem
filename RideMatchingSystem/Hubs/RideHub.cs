using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RideMatchingSystem.DTOs;
using RideMatchingSystem.Services;

namespace RideMatchingSystem.Hubs
{
    public class RideHub : Hub
    {
        private readonly DriverService _driverService;
        private readonly RideService _rideService;
        private readonly MatchingService _matchingService;
        private readonly ILogger<RideHub> _logger;

        public RideHub(DriverService driverService, RideService rideService, MatchingService matchingService, ILogger<RideHub> logger)
        {
            _driverService = driverService;
            _rideService = rideService;
            _matchingService = matchingService;
            _logger = logger;
        }

        // Driver sends periodic location updates
        public async Task UpdateLocation(int driverId, LocationDto location)
        {
            if (location == null) return;

            await _driverService.UpdateLocationAsync(driverId, location.Latitude, location.Longitude);
            await Clients.Others.SendAsync("DriverLocationUpdated", new { DriverId = driverId, location.Latitude, location.Longitude });
        }

        // Driver toggles availability
        public async Task SetAvailability(int driverId, bool isAvailable)
        {
            await _driverService.SetAvailabilityAsync(driverId, isAvailable);
            await Clients.All.SendAsync("DriverAvailabilityChanged", new { DriverId = driverId, IsAvailable = isAvailable });
        }

        // Subscribe to ride-specific updates
        public Task SubscribeRide(int rideId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, GetRideGroupName(rideId));
        }

        public Task UnsubscribeRide(int rideId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRideGroupName(rideId));
        }

        // Send ride update to subscribers of the ride
        public Task NotifyRideUpdated(RideDto ride)
        {
            if (ride == null) return Task.CompletedTask;
            return Clients.Group(GetRideGroupName(ride.Id)).SendAsync("RideUpdated", ride);
        }

        private static string GetRideGroupName(int rideId) => $"ride-{rideId}";
    }
}

