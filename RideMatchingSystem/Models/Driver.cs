using System;
using System.ComponentModel.DataAnnotations;

namespace RideMatchingSystem.Models
{
    public class Driver
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        // Current location (latitude, longitude)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Indicates whether the driver is currently available for matching
        public bool IsAvailable { get; set; }

        // Last time driver sent a location update
        public DateTime LastSeenUtc { get; set; }

        // Concurrency token for optimistic concurrency control
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
