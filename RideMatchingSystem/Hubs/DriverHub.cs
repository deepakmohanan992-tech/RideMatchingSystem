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
        private readonly IConnectionRegistry _registry;
        private readonly IOfferStore _offerStore;

        public DriverHub(DriverService driverService, MatchingService matchingService, RideService rideService, IConnectionRegistry registry, IOfferStore offerStore, ILogger<DriverHub> logger)
        {
            _driverService = driverService;
            _matchingService = matchingService;
            _rideService = rideService;
            _registry = registry;
            _offerStore = offerStore;
            _logger = logger;
        }

        public async Task UpdateLocation(DriverLocationUpdateDto location)
        {
            if (location == null) return;
            await _driverService.UpdateLocationAsync(location.DriverId, location.Latitude, location.Longitude);
            // Broadcast to other connected clients that a driver moved
            await Clients.Others.DriverLocationUpdated(location);
        }

        public override Task OnConnectedAsync()
        {
            // No automatic mapping yet. Clients should call Register to map their driverId -> connection
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Remove connection mapping from registry
            _registry.RemoveByConnection(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        // Drivers should call this after connecting to associate their driver id with the connection
        public Task Register(int driverId)
        {
            _registry.Add(driverId, Context.ConnectionId);
            return Task.CompletedTask;
        }

        public async Task SetAvailability(DriverAvailabilityDto availability)
        {
            await _driverService.SetAvailabilityAsync(availability.DriverId, availability.IsAvailable);
            await Clients.All.DriverAvailabilityChanged(availability);
        }

        public async Task AcceptOffer(string offerId)
        {
            _logger.LogInformation("Driver accepted offer {OfferId} (connection {Conn})", offerId, Context.ConnectionId);

            // Look up the offer and attempt to complete assignment
            if (!_offerStore.TryGet(offerId, out var offer))
            {
                await Clients.Caller.OfferCancelled(offerId);
                return;
            }

            var result = await _matchingService.AcceptOfferAsync(offerId, offer.DriverId);
            if (result)
            {
                await Clients.Caller.NotifyMessage($"Offer {offerId} accepted and ride assigned");
            }
            else
            {
                await Clients.Caller.OfferCancelled(offerId);
            }
        }

        public async Task DeclineOffer(string offerId)
        {
            _logger.LogInformation("Driver declined offer {OfferId} (connection {Conn})", offerId, Context.ConnectionId);
            await Clients.Caller.NotifyMessage($"Declined offer {offerId}");
        }
    }
}
