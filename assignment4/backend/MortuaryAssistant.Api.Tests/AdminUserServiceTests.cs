using Microsoft.AspNetCore.Identity;
using Moq;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;
using Xunit;

namespace MortuaryAssistant.Api.Tests;

public class AdminUserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly AdminUserService _adminUserService;

    public AdminUserServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _adminUserService = new AdminUserService(_mockUserManager.Object);
    }

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "user-1", Email = "user1@example.com", UserName = "user1@example.com", DisplayName = "User 1", LockoutEnd = null },
            new() { Id = "user-2", Email = "user2@example.com", UserName = "user2@example.com", DisplayName = "User 2", LockoutEnd = null },
        };

        var usersQueryable = users.AsQueryable();
        var mockDbSet = new Mock<IQueryable<ApplicationUser>>();
        mockDbSet.Setup(x => x.Provider).Returns(usersQueryable.Provider);
        mockDbSet.Setup(x => x.Expression).Returns(usersQueryable.Expression);
        mockDbSet.Setup(x => x.ElementType).Returns(usersQueryable.ElementType);
        mockDbSet.Setup(x => x.GetEnumerator()).Returns(usersQueryable.GetEnumerator());

        _mockUserManager.Setup(x => x.Users).Returns(mockDbSet.Object);

        _mockUserManager
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult(users.FirstOrDefault(u => u.Id == id)));

        _mockUserManager
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Mortician" });

        // Act
        var result = await _adminUserService.GetUsersAsync();

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value);
        var userList = result.Value.ToList();
        Assert.Equal(2, userList.Count);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsEmptyList_WhenNoUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>();
        var usersQueryable = users.AsQueryable();
        var mockDbSet = new Mock<IQueryable<ApplicationUser>>();
        mockDbSet.Setup(x => x.Provider).Returns(usersQueryable.Provider);
        mockDbSet.Setup(x => x.Expression).Returns(usersQueryable.Expression);
        mockDbSet.Setup(x => x.ElementType).Returns(usersQueryable.ElementType);
        mockDbSet.Setup(x => x.GetEnumerator()).Returns(usersQueryable.GetEnumerator());

        _mockUserManager.Setup(x => x.Users).Returns(mockDbSet.Object);

        // Act
        var result = await _adminUserService.GetUsersAsync();

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        var userList = result.Value!.ToList();
        Assert.Empty(userList);
    }

    [Fact]
    public async Task GetUsersAsync_IncludesDisabledStatus()
    {
        // Arrange
        var disabledUser = new ApplicationUser
        {
            Id = "user-1",
            Email = "disabled@example.com",
            UserName = "disabled@example.com",
            DisplayName = "Disabled User",
            LockoutEnd = DateTimeOffset.UtcNow.AddYears(100)
        };

        var enabledUser = new ApplicationUser
        {
            Id = "user-2",
            Email = "enabled@example.com",
            UserName = "enabled@example.com",
            DisplayName = "Enabled User",
            LockoutEnd = null
        };

        var users = new List<ApplicationUser> { disabledUser, enabledUser };
        var usersQueryable = users.AsQueryable();
        var mockDbSet = new Mock<IQueryable<ApplicationUser>>();
        mockDbSet.Setup(x => x.Provider).Returns(usersQueryable.Provider);
        mockDbSet.Setup(x => x.Expression).Returns(usersQueryable.Expression);
        mockDbSet.Setup(x => x.ElementType).Returns(usersQueryable.ElementType);
        mockDbSet.Setup(x => x.GetEnumerator()).Returns(usersQueryable.GetEnumerator());

        _mockUserManager.Setup(x => x.Users).Returns(mockDbSet.Object);

        _mockUserManager
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult(users.FirstOrDefault(u => u.Id == id)));

        _mockUserManager
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _adminUserService.GetUsersAsync();

        // Assert
        Assert.True(result.Ok);
        var userList = result.Value!.ToList();
        Assert.Equal(2, userList.Count);
    }

    #endregion

    #region SetRoleAsync Tests

    [Fact]
    public async Task SetRoleAsync_WithValidAdminRole_AddsRoleSuccessfully()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsInRoleAsync(user, Roles.Admin))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(user, Roles.Admin))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { Roles.Admin });

        // Act
        var result = await _adminUserService.SetRoleAsync(userId, Roles.Admin, true);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        _mockUserManager.Verify(x => x.AddToRoleAsync(user, Roles.Admin), Times.Once);
    }

    [Fact]
    public async Task SetRoleAsync_WithValidMorticianRole_AddsRoleSuccessfully()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsInRoleAsync(user, Roles.Mortician))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(user, Roles.Mortician))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { Roles.Mortician });

        // Act
        var result = await _adminUserService.SetRoleAsync(userId, Roles.Mortician, true);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task SetRoleAsync_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        var userId = "user-1";

        // Act
        var result = await _adminUserService.SetRoleAsync(userId, "InvalidRole", true);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid role.", result.Error);
    }

    [Fact]
    public async Task SetRoleAsync_WithNonexistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "nonexistent-user";

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _adminUserService.SetRoleAsync(userId, Roles.Admin, true);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task SetRoleAsync_RemovesRoleWhenEnabledIsFalse()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsInRoleAsync(user, Roles.Admin))
            .ReturnsAsync(true);

        _mockUserManager
            .Setup(x => x.RemoveFromRoleAsync(user, Roles.Admin))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _adminUserService.SetRoleAsync(userId, Roles.Admin, false);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        _mockUserManager.Verify(x => x.RemoveFromRoleAsync(user, Roles.Admin), Times.Once);
    }

    [Fact]
    public async Task SetRoleAsync_WithAddRoleFailure_ReturnsBadRequest()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };
        var identityError = new IdentityError { Code = "RoleError", Description = "Unable to add role" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsInRoleAsync(user, Roles.Admin))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(user, Roles.Admin))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _adminUserService.SetRoleAsync(userId, Roles.Admin, true);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Unable to add role", result.Error);
    }

    #endregion

    #region SetEnabledAsync Tests

    [Fact]
    public async Task SetEnabledAsync_WithEnabledTrue_ClearsLockout()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.GetLockoutEnabledAsync(user))
            .ReturnsAsync(true);

        _mockUserManager
            .Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _adminUserService.SetEnabledAsync(userId, true);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null), Times.Once);
    }

    [Fact]
    public async Task SetEnabledAsync_WithEnabledFalse_LocksOutUser()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.GetLockoutEnabledAsync(user))
            .ReturnsAsync(true);

        _mockUserManager
            .Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _adminUserService.SetEnabledAsync(userId, false);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        _mockUserManager.Verify(
            x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.Is<DateTimeOffset?>(d => d.HasValue && d.Value.Year >= 2125)), 
            Times.Once);
    }

    [Fact]
    public async Task SetEnabledAsync_WithNonexistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "nonexistent-user";

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _adminUserService.SetEnabledAsync(userId, true);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task SetEnabledAsync_EnablesLockoutIfNotEnabled()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.GetLockoutEnabledAsync(user))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.SetLockoutEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _adminUserService.SetEnabledAsync(userId, true);

        // Assert
        Assert.True(result.Ok);
        _mockUserManager.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidUser_DeletesSuccessfully()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _adminUserService.DeleteUserAsync("admin-user", userId);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(204, result.StatusCode);
        _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithNonexistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "nonexistent-user";

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _adminUserService.DeleteUserAsync("admin-user", userId);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("User not found.", result.Error);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenDeletingOwnAccount_ReturnsBadRequest()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _adminUserService.DeleteUserAsync(userId, userId);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("You cannot delete your own account while logged in.", result.Error);
    }

    [Fact]
    public async Task DeleteUserAsync_WithDeleteFailure_ReturnsBadRequest()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };
        var identityError = new IdentityError { Code = "DeleteError", Description = "Unable to delete user" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _adminUserService.DeleteUserAsync("admin-user", userId);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Unable to delete user", result.Error);
    }

    [Fact]
    public async Task DeleteUserAsync_AllowsDeletionWhenCurrentUserIsEmpty()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _adminUserService.DeleteUserAsync("", userId);

        // Assert
        Assert.True(result.Ok);
        _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    #endregion
}
