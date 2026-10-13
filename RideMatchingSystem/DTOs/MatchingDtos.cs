using System;

namespace RideMatchingSystem.DTOs
{
    // Use string status in DTOs to avoid ambiguity with domain enums
    public record RideResponseDto(string RideId, int RiderId, string Status, LocationDto Pickup, LocationDto? Dropoff, DateTime CreatedAt, int Version);

    public record RideStatusUpdateDto(string RideId, string Status, int ExpectedVersion);

    public record RideMatchOfferDto(string OfferId, string RideId, int DriverId, double EtaSeconds, LocationDto DriverLocation, decimal EstimatedFare, DateTime ExpiresAt);

    public record DriverDecisionDto([property: System.ComponentModel.DataAnnotations.Required(ErrorMessage = "OfferId is required")] string OfferId,
                                     [property: System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "DriverId must be greater than zero")] int DriverId,
                                     bool Accept);
}
