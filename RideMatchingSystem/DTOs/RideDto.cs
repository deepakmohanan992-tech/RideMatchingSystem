namespace RideMatchingSystem.DTOs
{
    public class RideDto
    {
        public int Id { get; set; }
        public int RiderId { get; set; }
        public int? DriverId { get; set; }
        public string? Status { get; set; }
    }
}
