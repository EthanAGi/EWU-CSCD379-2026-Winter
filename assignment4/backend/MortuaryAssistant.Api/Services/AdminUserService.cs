using Microsoft.AspNetCore.Identity;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Models;

namespace MortuaryAssistant.Api.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _users;

    public AdminUserService(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<ServiceResult<IEnumerable<object>>> GetUsersAsync()
    {
        // Query first (fast)
        var list = _users.Users
            .Select(u => new { u.Id, u.Email, u.UserName, u.DisplayName, u.LockoutEnd })
            .ToList();

        // Then per-user roles (Identity API)
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

        return ServiceResult<IEnumerable<object>>.Success(result, 200);
    }

    public async Task<ServiceResult<object>> SetRoleAsync(string userId, string role, bool enabled)
    {
        if (role != Roles.Admin && role != Roles.Mortician)
            return ServiceResult<object>.Fail("Invalid role.", 400);

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<object>.Fail("User not found.", 404);

        if (enabled)
        {
            if (!await _users.IsInRoleAsync(user, role))
            {
                var add = await _users.AddToRoleAsync(user, role);
                if (!add.Succeeded)
                    return ServiceResult<object>.Fail(string.Join("; ", add.Errors.Select(e => e.Description)), 400);
            }
        }
        else
        {
            if (await _users.IsInRoleAsync(user, role))
            {
                var remove = await _users.RemoveFromRoleAsync(user, role);
                if (!remove.Succeeded)
                    return ServiceResult<object>.Fail(string.Join("; ", remove.Errors.Select(e => e.Description)), 400);
            }
        }

        var roles = await _users.GetRolesAsync(user);

        return ServiceResult<object>.Success(new
        {
            user.Id,
            user.Email,
            Roles = roles.ToArray()
        }, 200);
    }

    public async Task<ServiceResult<object>> SetEnabledAsync(string userId, bool enabled)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<object>.Fail("User not found.", 404);

        // Ensure lockout is enabled so LockoutEnd works
        var lockoutEnabled = await _users.GetLockoutEnabledAsync(user);
        if (!lockoutEnabled)
        {
            var enabledRes = await _users.SetLockoutEnabledAsync(user, true);
            if (!enabledRes.Succeeded)
                return ServiceResult<object>.Fail(string.Join("; ", enabledRes.Errors.Select(e => e.Description)), 400);
        }

        IdentityResult setRes;
        if (enabled)
        {
            // Enable: clear lockout
            setRes = await _users.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            // Disable: lock out far into the future
            setRes = await _users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }

        if (!setRes.Succeeded)
            return ServiceResult<object>.Fail(string.Join("; ", setRes.Errors.Select(e => e.Description)), 400);

        var roles = await _users.GetRolesAsync(user);

        // re-fetch to return latest lockout
        var updated = await _users.FindByIdAsync(user.Id);
        var lockoutEnd = updated?.LockoutEnd;

        var isDisabled =
            lockoutEnd.HasValue &&
            lockoutEnd.Value.UtcDateTime > DateTime.UtcNow;

        return ServiceResult<object>.Success(new
        {
            user.Id,
            user.Email,
            Roles = roles.ToArray(),
            IsDisabled = isDisabled,
            LockoutEndUtc = lockoutEnd
        }, 200);
    }

    public async Task<ServiceResult<object>> DeleteUserAsync(string currentUserId, string userIdToDelete)
    {
        var user = await _users.FindByIdAsync(userIdToDelete);
        if (user is null)
            return ServiceResult<object>.Fail("User not found.", 404);

        if (!string.IsNullOrWhiteSpace(currentUserId) && currentUserId == user.Id)
            return ServiceResult<object>.Fail("You cannot delete your own account while logged in.", 400);

        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded)
            return ServiceResult<object>.Fail(string.Join("; ", result.Errors.Select(e => e.Description)), 400);

        // Controller will translate this to 204
        return ServiceResult<object>.Success(new { message = "User deleted." }, 204);
    }
}