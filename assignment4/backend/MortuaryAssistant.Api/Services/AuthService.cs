using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MortuaryAssistant.Api.Models;

namespace MortuaryAssistant.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return ServiceResult<AuthResponse>.Fail("Email and password are required.", 400);

        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing is not null)
            return ServiceResult<AuthResponse>.Fail("Email already registered.", 409);

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName ?? req.Email
        };

        var created = await _userManager.CreateAsync(user, req.Password);
        if (!created.Succeeded)
            return ServiceResult<AuthResponse>.Fail(string.Join("; ", created.Errors.Select(e => e.Description)), 400);

        // default role (optional)
        if (await _userManager.IsInRoleAsync(user, "Mortician") == false &&
            await _userManager.IsInRoleAsync(user, "Admin") == false)
        {
            // If your app seeds roles, this is safe. If not, you can remove this.
            await _userManager.AddToRoleAsync(user, "Mortician");
        }

        var resp = await BuildAuthResponse(user);
        return ServiceResult<AuthResponse>.Success(resp, 200);
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return ServiceResult<AuthResponse>.Fail("Email and password are required.", 400);

        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return ServiceResult<AuthResponse>.Fail("Invalid login.", 401);

        if (!await _userManager.CheckPasswordAsync(user, req.Password))
            return ServiceResult<AuthResponse>.Fail("Invalid login.", 401);

        var resp = await BuildAuthResponse(user);
        return ServiceResult<AuthResponse>.Success(resp, 200);
    }

    private async Task<AuthResponse> BuildAuthResponse(ApplicationUser user)
    {
        // These should be provided in Azure App Settings:
        // Jwt__Key, Jwt__Issuer, Jwt__Audience
        var jwtKey = _config["Jwt:Key"] ?? "dev-secret-change-me";
        var jwtIssuer = _config["Jwt:Issuer"] ?? "MortuaryAssistant";

        // ✅ FIX: read audience and use it when minting the token
        // If Jwt:Audience is not set, fall back to issuer to avoid breaking dev defaults.
        var jwtAudience = _config["Jwt:Audience"] ?? jwtIssuer;

        var expires = DateTime.UtcNow.AddHours(8);

        var roles = await _userManager.GetRolesAsync(user);

        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email ?? ""),

            // Optional, but nice to have for diagnostics / uniqueness
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience, // ✅ FIXED HERE
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(
            Token: tokenString,
            ExpiresAtUtc: expires,
            Email: user.Email ?? "",
            DisplayName: user.DisplayName,
            Roles: roles.ToArray()
        );
    }
}