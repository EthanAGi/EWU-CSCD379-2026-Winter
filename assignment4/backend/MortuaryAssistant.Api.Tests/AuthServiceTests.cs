using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;
using Xunit;

namespace MortuaryAssistant.Api.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(x => x["Jwt:Key"]).Returns("my-super-secret-key-that-is-long-enough");
        _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("MortuaryAssistant");
        _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("MortuaryAssistant");

        _authService = new AuthService(_mockUserManager.Object, _mockConfig.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var registerRequest = new RegisterRequest("test@example.com", "Password123!", "Test User");
        _mockUserManager
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync((ApplicationUser)null!);

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Mortician"))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Mortician" });

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.NotEmpty(result.Value.Token);
    }

    [Fact]
    public async Task RegisterAsync_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest("", "Password123!", "Test User");

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email and password are required.", result.Error);
    }

    [Fact]
    public async Task RegisterAsync_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest("test@example.com", "", "Test User");

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email and password are required.", result.Error);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var existingUser = new ApplicationUser { Email = "test@example.com" };
        var registerRequest = new RegisterRequest("test@example.com", "Password123!", "Test User");

        _mockUserManager
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Email already registered.", result.Error);
    }

    [Fact]
    public async Task RegisterAsync_WithCreateFailure_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest("test@example.com", "Password123!", "Test User");
        var identityError = new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync((ApplicationUser)null!);

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Password is too short", result.Error);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        var loginRequest = new LoginRequest("test@example.com", "Password123!");

        _mockUserManager
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.CheckPasswordAsync(user, "Password123!"))
            .ReturnsAsync(true);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Mortician" });

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.NotEmpty(result.Value.Token);
    }

    [Fact]
    public async Task LoginAsync_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest("", "Password123!");

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email and password are required.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest("test@example.com", "");

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email and password are required.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent@example.com", "Password123!");

        _mockUserManager
            .Setup(x => x.FindByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("Invalid login.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = "test@example.com"
        };
        var loginRequest = new LoginRequest("test@example.com", "WrongPassword!");

        _mockUserManager
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.CheckPasswordAsync(user, "WrongPassword!"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("Invalid login.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_WithMultipleRoles_IncludesAllRolesInToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = "test@example.com",
            DisplayName = "Admin User"
        };
        var loginRequest = new LoginRequest("test@example.com", "Password123!");
        var roles = new List<string> { "Admin", "Mortician" };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.CheckPasswordAsync(user, "Password123!"))
            .ReturnsAsync(true);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.Ok);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Roles.Length);
        Assert.Contains("Admin", result.Value.Roles);
        Assert.Contains("Mortician", result.Value.Roles);
    }

    #endregion
}
