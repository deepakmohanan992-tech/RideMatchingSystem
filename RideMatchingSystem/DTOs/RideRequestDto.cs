namespace RideMatchingSystem.DTOs
{
    public class RideRequestDto
    {
        // Optional Id for upsert scenarios - when provided the API will update the existing ride
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Id must be greater than zero when provided")]
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "RiderId must be greater than zero")]
        public int RiderId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "PickupLocation is required")]
        public LocationDto PickupLocation { get; set; } = null!;

        public LocationDto? DropoffLocation { get; set; }
    }
}
