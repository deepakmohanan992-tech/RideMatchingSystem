using System;

namespace RideMatchingSystem.Models
{
    public enum RideStatus
    {
        Requested,
        Assigned,
        InProgress,
        Completed,
        Cancelled
    }

    public class Ride
    {
        public int Id { get; set; }

        // Foreign keys
        public int RiderId { get; set; }
        public int? DriverId { get; set; }

        // Pickup/Dropoff locations
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public double? DropoffLatitude { get; set; }
        public double? DropoffLongitude { get; set; }

        public RideStatus Status { get; set; } = RideStatus.Requested;

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? AssignedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
