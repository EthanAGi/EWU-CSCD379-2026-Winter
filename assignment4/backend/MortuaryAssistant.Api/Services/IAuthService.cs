using MortuaryAssistant.Api.Models;

namespace MortuaryAssistant.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest req);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest req);
}