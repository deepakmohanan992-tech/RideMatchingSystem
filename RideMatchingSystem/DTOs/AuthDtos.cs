namespace RideMatchingSystem.DTOs
{
    public record AuthRegisterRequest(string Name, string Email, string Password, string Role);
    public record AuthLoginRequest(string Email, string Password);
    public record AuthResponse(string AccessToken, System.DateTime ExpiresAt, string UserId, string Role);
}
