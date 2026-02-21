namespace MortuaryAssistant.Api.Models;

public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string Email,
    string? DisplayName,
    string[] Roles
);