namespace MortuaryAssistant.Api.Services;

public interface IAdminUserService
{
    Task<ServiceResult<IEnumerable<object>>> GetUsersAsync();
    Task<ServiceResult<object>> SetRoleAsync(string userId, string role, bool enabled);
    Task<ServiceResult<object>> SetEnabledAsync(string userId, bool enabled);
    Task<ServiceResult<object>> DeleteUserAsync(string currentUserId, string userIdToDelete);
}