namespace RideMatchingSystem.DTOs
{
    public class RiderDto
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Rider name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Rider name must be at most 100 characters")]
        public string? Name { get; set; }
    }
}
