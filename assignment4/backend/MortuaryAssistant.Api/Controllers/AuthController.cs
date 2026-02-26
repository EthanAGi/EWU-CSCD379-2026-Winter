using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly UserManager<ApplicationUser> _users;

    public AuthController(IAuthService auth, UserManager<ApplicationUser> users)
    {
        _auth = auth;
        _users = users;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var result = await _auth.RegisterAsync(req);
        return ToActionResult(result, endpoint: "register");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var result = await _auth.LoginAsync(req);
        return ToActionResult(result, endpoint: "login");
    }

    // IAuthService only defines Register/Login, so "me" stays in the controller.
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);

        return Ok(new
        {
            user.Email,
            user.DisplayName,
            Roles = roles.ToArray()
        });
    }

    private static IActionResult ToActionResult(ServiceResult<AuthResponse> result, string endpoint)
    {
        // Your ServiceResult<T> uses: Ok / Value / Error
        if (result.Ok && result.Value is not null)
            return new OkObjectResult(result.Value);

        var msg = result.Error ?? "Request failed.";

        // Pick sensible status codes based on the AuthService errors you uploaded
        if (endpoint == "login" && msg.Contains("Invalid email/password", StringComparison.OrdinalIgnoreCase))
            return new UnauthorizedObjectResult(new { message = msg });

        if (endpoint == "register" && msg.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            return new BadRequestObjectResult(new { message = msg });

        return new BadRequestObjectResult(new { message = msg });
    }
}