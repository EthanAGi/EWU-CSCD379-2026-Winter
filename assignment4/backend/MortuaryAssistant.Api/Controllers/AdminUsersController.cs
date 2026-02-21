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

    // ----------------------------
    // DTOs / Request Models
    // ----------------------------
    public record SetRoleRequest(string UserId, string Role, bool Enabled);

    public record SetEnabledRequest(string UserId, bool Enabled);

    // ----------------------------
    // GET: /api/admin/users
    // Now returns: Id, Email, UserName, DisplayName, Roles, IsDisabled, LockoutEndUtc
    // ----------------------------
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> ListUsers()
    {
        // Pull basic user list first (query to DB)
        var list = _users.Users
            .Select(u => new { u.Id, u.Email, u.UserName, u.DisplayName, u.LockoutEnd })
            .ToList();

        // Then fetch roles per user (Identity API is async per user)
        var result = new List<object>(list.Count);

        foreach (var u in list)
        {
            var user = await _users.FindByIdAsync(u.Id);
            if (user is null) continue;

            var roles = await _users.GetRolesAsync(user);

            var lockoutEnd = u.LockoutEnd;
            var isDisabled =
                lockoutEnd.HasValue &&
                lockoutEnd.Value.UtcDateTime > DateTime.UtcNow;

            result.Add(new
            {
                u.Id,
                u.Email,
                u.UserName,
                u.DisplayName,
                Roles = roles.ToArray(),
                IsDisabled = isDisabled,
                LockoutEndUtc = lockoutEnd
            });
        }

        return Ok(result);
    }

    // ----------------------------
    // POST: /api/admin/users/set-role
    // (your existing endpoint)
    // ----------------------------
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

    // ----------------------------
    // POST: /api/admin/users/set-enabled
    // Enable/Disable user account (using LockoutEnd)
    // ----------------------------
    [HttpPost("set-enabled")]
    public async Task<IActionResult> SetEnabled(SetEnabledRequest req)
    {
        var user = await _users.FindByIdAsync(req.UserId);
        if (user is null) return NotFound(new { message = "User not found." });

        // Ensure lockout is enabled so LockoutEnd works
        await _users.SetLockoutEnabledAsync(user, true);

        if (req.Enabled)
        {
            // Enable account: clear lockout
            await _users.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            // Disable account: lock out far into the future
            await _users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }

        var roles = await _users.GetRolesAsync(user);

        // Refresh lockout end from the updated user
        var updated = await _users.FindByIdAsync(user.Id);
        var lockoutEnd = updated?.LockoutEnd;

        var isDisabled =
            lockoutEnd.HasValue &&
            lockoutEnd.Value.UtcDateTime > DateTime.UtcNow;

        return Ok(new
        {
            user.Id,
            user.Email,
            Roles = roles.ToArray(),
            IsDisabled = isDisabled,
            LockoutEndUtc = lockoutEnd
        });
    }

    // ----------------------------
    // DELETE: /api/admin/users/{userId}
    // Deletes the user
    // ----------------------------
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return NotFound(new { message = "User not found." });

        // Optional safety: prevent deleting yourself
        var currentUserId = _users.GetUserId(User);
        if (!string.IsNullOrWhiteSpace(currentUserId) && currentUserId == user.Id)
        {
            return BadRequest(new { message = "You cannot delete your own account while logged in." });
        }

        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = string.Join("; ", result.Errors.Select(e => e.Description))
            });
        }

        return NoContent();
    }
}