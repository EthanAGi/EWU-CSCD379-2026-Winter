using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Models;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;

    public AdminUsersController(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    [HttpGet]
    public ActionResult<IEnumerable<object>> ListUsers()
    {
        // Keep it simple: show basics. (Avoid dumping everything.)
        var data = _users.Users
            .Select(u => new { u.Id, u.Email, u.UserName, u.DisplayName })
            .ToList();

        return Ok(data);
    }

    public record SetRoleRequest(string UserId, string Role, bool Enabled);

    [HttpPost("set-role")]
    public async Task<IActionResult> SetRole(SetRoleRequest req)
    {
        if (req.Role != Roles.Admin && req.Role != Roles.Mortician)
            return BadRequest(new { message = "Invalid role." });

        var user = await _users.FindByIdAsync(req.UserId);
        if (user is null) return NotFound(new { message = "User not found." });

        if (req.Enabled)
        {
            if (!await _users.IsInRoleAsync(user, req.Role))
                await _users.AddToRoleAsync(user, req.Role);
        }
        else
        {
            if (await _users.IsInRoleAsync(user, req.Role))
                await _users.RemoveFromRoleAsync(user, req.Role);
        }

        var roles = await _users.GetRolesAsync(user);
        return Ok(new { user.Id, user.Email, Roles = roles.ToArray() });
    }
}