namespace RideMatchingSystem.DTOs
{
    public class ApiResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public T? Result { get; set; }
    }
}
