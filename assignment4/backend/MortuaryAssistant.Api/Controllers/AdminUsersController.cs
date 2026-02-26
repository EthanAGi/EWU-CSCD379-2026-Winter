using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _svc;
    private readonly UserManager<ApplicationUser> _users; // only used for GetUserId(User)

    public AdminUsersController(IAdminUserService svc, UserManager<ApplicationUser> users)
    {
        _svc = svc;
        _users = users;
    }

    public record SetRoleRequest(string UserId, string Role, bool Enabled);
    public record SetEnabledRequest(string UserId, bool Enabled);

    [HttpGet]
    public async Task<IActionResult> ListUsers()
    {
        var res = await _svc.GetUsersAsync();
        return ToActionResult(res);
    }

    [HttpPost("set-role")]
    public async Task<IActionResult> SetRole(SetRoleRequest req)
    {
        var res = await _svc.SetRoleAsync(req.UserId, req.Role, req.Enabled);
        return ToActionResult(res);
    }

    [HttpPost("set-enabled")]
    public async Task<IActionResult> SetEnabled(SetEnabledRequest req)
    {
        var res = await _svc.SetEnabledAsync(req.UserId, req.Enabled);
        return ToActionResult(res);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var currentUserId = _users.GetUserId(User) ?? "";
        var res = await _svc.DeleteUserAsync(currentUserId, userId);

        // if service returns 204, we should return NoContent()
        if (res.Ok && res.StatusCode == 204) return NoContent();

        return ToActionResult(res);
    }

    private IActionResult ToActionResult<T>(ServiceResult<T> res)
    {
        if (res.Ok)
        {
            // For "successful but no body", your service can return status 204,
            // but we already handle that in DeleteUser above.
            return StatusCode(res.StatusCode, res.Value);
        }

        // Error shape matches your controllers: { message = "..." }
        return StatusCode(res.StatusCode, new { message = res.Error });
    }
}