namespace RideMatchingSystem.DTOs
{
    public record DriverLocationUpdateDto([property: System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "DriverId must be greater than zero")] int DriverId,
                                            [property: System.ComponentModel.DataAnnotations.Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")] double Latitude,
                                            [property: System.ComponentModel.DataAnnotations.Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")] double Longitude,
                                            System.DateTime Timestamp);

    public record DriverAvailabilityDto([property: System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "DriverId must be greater than zero")] int DriverId, bool IsAvailable);
    public record NearbyDriverSummaryDto(int DriverId, double DistanceMeters, LocationDto Location, decimal Rating);
    public record RatingDto(string RideId, string FromUserId, string ToUserId, int Stars, string? Comments);
}
