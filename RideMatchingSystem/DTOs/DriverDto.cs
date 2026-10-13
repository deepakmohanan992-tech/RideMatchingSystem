namespace RideMatchingSystem.DTOs
{
    public class DriverDto
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Driver name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Driver name must be at most 100 characters")] 
        public string? Name { get; set; }
        public LocationDto? Location { get; set; }
        public bool IsAvailable { get; set; }
    }
}
