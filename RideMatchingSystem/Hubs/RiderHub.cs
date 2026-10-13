using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RideMatchingSystem.DTOs;

namespace RideMatchingSystem.Hubs
{
    public interface IRiderClient
    {
        Task RideMatched(RideMatchOfferDto assignment);
        Task DriverLocationUpdate(DriverLocationUpdateDto location);
        Task RideStatusUpdated(RideResponseDto ride);
        Task NotifyMessage(string message);
    }

    public interface IRiderServer
    {
        Task SubscribeRide(string rideId);
        Task UnsubscribeRide(string rideId);
    }

    public class RiderHub : Hub<IRiderClient>, IRiderServer
    {
        public Task SubscribeRide(string rideId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, GetRideGroupName(rideId));
        }

        public Task UnsubscribeRide(string rideId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRideGroupName(rideId));
        }

        private static string GetRideGroupName(string rideId) => $"ride:{rideId}";
    }
}
