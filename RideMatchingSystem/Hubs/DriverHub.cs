using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RideMatchingSystem.DTOs;
using RideMatchingSystem.Services;

namespace RideMatchingSystem.Hubs
{
    // Methods server calls on driver clients
    public interface IDriverClient
    {
        Task ReceiveRideOffer(RideMatchOfferDto offer);
        Task OfferCancelled(string offerId);
        Task RideAssigned(RideResponseDto ride);
        Task DriverLocationUpdated(DriverLocationUpdateDto location);
        Task DriverAvailabilityChanged(DriverAvailabilityDto availability);
        Task NotifyMessage(string message);
    }

    // Methods driver client can call on the hub
    public interface IDriverServer
    {
        Task UpdateLocation(DriverLocationUpdateDto location);
        Task SetAvailability(DriverAvailabilityDto availability);
        Task AcceptOffer(string offerId);
        Task DeclineOffer(string offerId);
    }

    public class DriverHub : Hub<IDriverClient>, IDriverServer
    {
        private readonly DriverService _driverService;
        private readonly MatchingService _matchingService;
        private readonly RideService _rideService;
        private readonly ILogger<DriverHub> _logger;

        public DriverHub(DriverService driverService, MatchingService matchingService, RideService rideService, ILogger<DriverHub> logger)
        {
            _driverService = driverService;
            _matchingService = matchingService;
            _rideService = rideService;
            _logger = logger;
        }

        public async Task UpdateLocation(DriverLocationUpdateDto location)
        {
            if (location == null) return;
            await _driverService.UpdateLocationAsync(location.DriverId, location.Latitude, location.Longitude);
            // Broadcast to other connected clients that a driver moved
            await Clients.Others.DriverLocationUpdated(location);
        }

        public async Task SetAvailability(DriverAvailabilityDto availability)
        {
            await _driverService.SetAvailabilityAsync(availability.DriverId, availability.IsAvailable);
            await Clients.All.DriverAvailabilityChanged(availability);
        }

        public async Task AcceptOffer(string offerId)
        {
            // In this simplified implementation we don't persist ephemeral offers.
            // Log and notify caller; matching/assignment is handled server-side via MatchingService and RideService.
            _logger.LogInformation("Driver accepted offer {OfferId} (connection {Conn})", offerId, Context.ConnectionId);
            await Clients.Caller.NotifyMessage($"Accepted offer {offerId}");
        }

        public async Task DeclineOffer(string offerId)
        {
            _logger.LogInformation("Driver declined offer {OfferId} (connection {Conn})", offerId, Context.ConnectionId);
            await Clients.Caller.NotifyMessage($"Declined offer {offerId}");
        }
    }
}
