# MortuaryAssistant API Unit Tests

This document describes the comprehensive unit test suite for the MortuaryAssistant API services.

## Overview

The test suite contains **37 tests** covering all service methods across three main services:
- **AuthService** (10 tests)
- **AdminUserService** (19 tests)  
- **CaseService** (8 tests)

All tests use xUnit for the testing framework, Moq for mocking, and Entity Framework Core's in-memory database for integration testing.

## Running the Tests

### Run all tests:
```bash
cd assignment4/backend/MortuaryAssistant.Api.Tests
dotnet test
```

### Run with verbose output:
```bash
dotnet test --verbosity normal
```

### Run a specific test class:
```bash
dotnet test --filter "ClassName=AuthServiceTests"
```

### Run a specific test method:
```bash
dotnet test --filter "Name=TestMethodName"
```

## Test Organization

### AuthServiceTests.cs (10 tests)

Tests for user registration and login functionality.

**RegisterAsync Tests:**
- ✅ `RegisterAsync_WithValidInput_ReturnsSuccess`: Verify successful user registration
- ✅ `RegisterAsync_WithMissingEmail_ReturnsBadRequest`: Email validation
- ✅ `RegisterAsync_WithMissingPassword_ReturnsBadRequest`: Password validation
- ✅ `RegisterAsync_WithExistingEmail_ReturnsConflict`: Prevent duplicate registrations
- ✅ `RegisterAsync_WithCreateFailure_ReturnsBadRequest`: Handle password complexity failures

**LoginAsync Tests:**
- ✅ `LoginAsync_WithValidCredentials_ReturnsSuccess`: Verify successful login
- ✅ `LoginAsync_WithMissingEmail_ReturnsBadRequest`: Email validation
- ✅ `LoginAsync_WithMissingPassword_ReturnsBadRequest`: Password validation
- ✅ `LoginAsync_WithNonexistentUser_ReturnsUnauthorized`: Invalid user handling
- ✅ `LoginAsync_WithWrongPassword_ReturnsUnauthorized`: Invalid password handling
- ✅ `LoginAsync_WithMultipleRoles_IncludesAllRolesInToken`: Verify JWT contains all user roles

### AdminUserServiceTests.cs (19 tests)

Tests for user management, role assignment, and enabling/disabling users.

**GetUsersAsync Tests:**
- ✅ `GetUsersAsync_ReturnsAllUsers`: Retrieve all users with their roles
- ✅ `GetUsersAsync_ReturnsEmptyList_WhenNoUsers`: Handle empty user list
- ✅ `GetUsersAsync_IncludesDisabledStatus`: Include lockout status in results

**SetRoleAsync Tests:**
- ✅ `SetRoleAsync_WithValidAdminRole_AddsRoleSuccessfully`: Add Admin role
- ✅ `SetRoleAsync_WithValidMorticianRole_AddsRoleSuccessfully`: Add Mortician role
- ✅ `SetRoleAsync_WithInvalidRole_ReturnsBadRequest`: Reject invalid roles
- ✅ `SetRoleAsync_WithNonexistentUser_ReturnsNotFound`: Handle non-existent users
- ✅ `SetRoleAsync_RemovesRoleWhenEnabledIsFalse`: Remove roles from users
- ✅ `SetRoleAsync_WithAddRoleFailure_ReturnsBadRequest`: Handle role assignment failures

**SetEnabledAsync Tests:**
- ✅ `SetEnabledAsync_WithEnabledTrue_ClearsLockout`: Enable disabled users
- ✅ `SetEnabledAsync_WithEnabledFalse_LocksOutUser`: Disable users (100-year lockout)
- ✅ `SetEnabledAsync_WithNonexistentUser_ReturnsNotFound`: Handle non-existent users
- ✅ `SetEnabledAsync_EnablesLockoutIfNotEnabled`: Activate lockout mechanism if needed

**DeleteUserAsync Tests:**
- ✅ `DeleteUserAsync_WithValidUser_DeletesSuccessfully`: Delete users
- ✅ `DeleteUserAsync_WithNonexistentUser_ReturnsNotFound`: Handle non-existent users
- ✅ `DeleteUserAsync_WhenDeletingOwnAccount_ReturnsBadRequest`: Prevent self-deletion
- ✅ `DeleteUserAsync_WithDeleteFailure_ReturnsBadRequest`: Handle deletion errors
- ✅ `DeleteUserAsync_AllowsDeletionWhenCurrentUserIsEmpty`: Allow deletion when no current user

### CaseServiceTests.cs (8 tests)

Tests for case file retrieval and management.

**GetAllCasesAsync Tests:**
- ✅ `GetAllCasesAsync_ReturnsAllCases`: Retrieve all cases
- ✅ `GetAllCasesAsync_ReturnsEmptyList_WhenNoCases`: Handle empty case list
- ✅ `GetAllCasesAsync_OrdersResultsByCreationDateDescending`: Verify correct ordering
- ✅ `GetAllCasesAsync_ReturnsCasesAsAnonymousObjects`: Verify anonymous object structure
- ✅ `GetAllCasesAsync_IncludesStatusAndAssignedMorticianUserId`: Verify required fields
- ✅ `GetAllCasesAsync_HandlesCaseWithNullAssignedMortician`: Handle null mortician IDs
- ✅ `GetAllCasesAsync_ReturnsMultipleCases`: Retrieve multiple cases
- ✅ `GetAllCasesAsync_ResultsUseNoTracking`: Verify EF Core no-tracking behavior

## Testing Patterns

### Mocking UserManager
Tests use Moq to mock `UserManager<ApplicationUser>` since it requires complex dependencies:

```csharp
var store = new Mock<IUserStore<ApplicationUser>>();
_mockUserManager = new Mock<UserManager<ApplicationUser>>(
    store.Object, null, null, null, null, null, null, null, null);
```

### In-Memory Database
CaseService tests use EF Core's in-memory database for integration testing:

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

_dbContext = new AppDbContext(options);
```

## Test Coverage Summary

| Service | Method | Tests | Status |
|---------|--------|-------|--------|
| AuthService | RegisterAsync | 5 | ✅ All Pass |
| AuthService | LoginAsync | 5 | ✅ All Pass |
| AdminUserService | GetUsersAsync | 3 | ✅ All Pass |
| AdminUserService | SetRoleAsync | 6 | ✅ All Pass |
| AdminUserService | SetEnabledAsync | 4 | ✅ All Pass |
| AdminUserService | DeleteUserAsync | 5 | ✅ All Pass |
| CaseService | GetAllCasesAsync | 8 | ✅ All Pass |
| **TOTAL** | | **37** | ✅ **All Pass** |

## Key Testing Principles Used

1. **Arrange-Act-Assert**: Every test follows the AAA pattern for clarity
2. **One Assertion Per Test**: Each test verifies a single behavior
3. **Descriptive Names**: Test names clearly describe what is being tested
4. **Isolation**: Tests are independent and don't depend on execution order
5. **Mocking External Dependencies**: UserManager is mocked to isolate service logic
6. **Real Database for Integration**: CaseService tests use real in-memory DB context

## Development Guidelines

When adding new services or modifying existing ones:

1. Add corresponding test methods in the appropriate test class
2. Name tests following the pattern: `MethodName_Scenario_ExpectedBehavior`
3. Use the same Arrange-Act-Assert pattern
4. Mock external dependencies (UserManager, IConfiguration, etc.)
5. Run tests locally before committing: `dotnet test`
6. Ensure all tests pass and new tests achieve >80% coverage

## Troubleshooting

If tests fail:

1. **Clean and rebuild**: `dotnet clean && dotnet build`
2. **Ensure database isolation**: Each test uses a unique in-memory database name
3. **Check mock setup**: Verify all required method mocks are configured
4. **Review service changes**: Ensure service logic matches test expectations

## CI/CD Integration

These tests can be integrated into your CI/CD pipeline:

```bash
dotnet test --verbosity normal --logger "trx;LogFileName=test-results.trx"
```

The `test-results.trx` file can be published to CI/CD platforms like Azure DevOps or GitHub Actions.
