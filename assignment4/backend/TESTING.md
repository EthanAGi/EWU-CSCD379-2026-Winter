# Backend Testing Guide

## Overview

The MortuaryAssistant API backend includes a comprehensive test suite with 37 unit tests covering all service-layer logic.

## Test Project Structure

```
MortuaryAssistant.Api.Tests/
├── MortuaryAssistant.Api.Tests.csproj
├── AuthServiceTests.cs
├── AdminUserServiceTests.cs
├── CaseServiceTests.cs
└── README.md
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio Code or Visual Studio

### Running Tests

**Run all tests:**
```bash
cd assignment4/backend/MortuaryAssistant.Api.Tests
dotnet test
```

**Run with detailed output:**
```bash
dotnet test --verbosity normal
```

**Run specific test:**
```bash
dotnet test --filter "Name~RegisterAsync_WithValidInput"
```

### Expected Output
```
Passed!  - Failed: 0, Passed: 37, Skipped: 0, Total: 37, Duration: 623 ms
```

## Test Statistics

- **Total Tests**: 37
- **All Passing**: ✅ Yes
- **Code Coverage**: All service methods covered
- **Framework**: xUnit
- **Mocking**: Moq
- **Database**: EF Core In-Memory

## Service Test Coverage

### Authentication & Authorization (10 tests)
- **File**: `AuthServiceTests.cs`
- **Service**: `IAuthService` (AuthService)
- **Endpoints Tested**:
  - POST /auth/register
  - POST /auth/login

### User Administration (19 tests)
- **File**: `AdminUserServiceTests.cs`
- **Service**: `IAdminUserService` (AdminUserService)
- **Endpoints Tested**:
  - GET /admin/users
  - POST/PUT /admin/users/{id}/role
  - POST/PUT /admin/users/{id}/enabled
  - DELETE /admin/users/{id}

### Case Management (8 tests)
- **File**: `CaseServiceTests.cs`
- **Service**: `ICaseService` (CaseService)
- **Endpoints Tested**:
  - GET /cases

## Test Scenarios

### AuthService Tests

**Registration Scenarios:**
1. Valid input with all required fields
2. Missing email validation
3. Missing password validation
4. Preventing duplicate email registrations
5. Handling identity password policy failures

**Login Scenarios:**
1. Successful login with valid credentials
2. Missing email validation
3. Missing password validation
4. Invalid user handling
5. Invalid password handling
6. JWT generation with multiple user roles

### AdminUserService Tests

**User Retrieval:**
1. Get all users with their roles
2. Handle empty user list
3. Include disabled status info

**Role Management:**
1. Add Admin role
2. Add Mortician role
3. Reject invalid roles
4. Prevent orphaned role assignments
5. Remove roles when disabling

**User Disabling/Enabling:**
1. Disable users (100-year lockout)
2. Re-enable users (clear lockout)
3. Activate lockout mechanism if needed
4. Handle non-existent users

**User Deletion:**
1. Delete valid users
2. Prevent self-deletion
3. Handle deletion errors
4. Verify cascading behavior

### CaseService Tests

**Case Retrieval:**
1. Return all cases
2. Handle empty case list
3. Order by creation date (descending)
4. Return anonymous objects with correct schema
5. Include all required fields
6. Handle null assigned mortician
7. Performance with multiple cases
8. Verify no-tracking for read operations

## Mocking Strategy

### UserManager Mocking
The tests mock `UserManager<ApplicationUser>` because:
- It requires extensive dependencies (IUserStore, IPasswordHasher, etc.)
- Tests focus on service logic, not identity framework
- Mocking allows testing error scenarios easily

**Example:**
```csharp
var store = new Mock<IUserStore<ApplicationUser>>();
_mockUserManager = new Mock<UserManager<ApplicationUser>>(
    store.Object, null, null, null, null, null, null, null, null);

_mockUserManager
    .Setup(x => x.FindByEmailAsync("test@example.com"))
    .ReturnsAsync(user);
```

### Configuration Mocking
For AuthService JWT configuration:
```csharp
_mockConfig.Setup(x => x["Jwt:Key"]).Returns("key-value");
_mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("issuer-value");
_mockConfig.Setup(x => x["Jwt:Audience"]).Returns("audience-value");
```

### Database Mocking
For CaseService, an in-memory database is used for integration testing:
```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
```

## Test Design Patterns

### Arrange-Act-Assert (AAA)
All tests follow this pattern:
```csharp
[Fact]
public async Task TestName()
{
    // Arrange: Set up test data and mocks
    var user = new ApplicationUser { ... };
    _mockUserManager.Setup(...).ReturnsAsync(...);
    
    // Act: Execute the code being tested
    var result = await _service.MethodAsync(...);
    
    // Assert: Verify the results
    Assert.True(result.Ok);
    Assert.Equal(expected, result.Value);
}
```

### ServiceResult Pattern
All services return `ServiceResult<T>` with status codes:
```csharp
public sealed class ServiceResult<T>
{
    public bool Ok { get; }
    public int StatusCode { get; }
    public T? Value { get; }
    public string? Error { get; }
}
```

Tests verify both `Ok` flag and HTTP status codes:
```csharp
Assert.True(result.Ok);
Assert.Equal(200, result.StatusCode);
Assert.NotNull(result.Value);
```

## Common Test Assertions

```csharp
// Verify success
Assert.True(result.Ok);
Assert.Equal(200, result.StatusCode);

// Verify failures
Assert.False(result.Ok);
Assert.Equal(400, result.StatusCode);
Assert.NotNull(result.Error);

// Verify data
Assert.Equal(expected, actual);
Assert.Contains(item, collection);
Assert.Single(collection);
Assert.Empty(collection);

// Verify mocks were called
_mockUserManager.Verify(x => x.FindByIdAsync(userId), Times.Once);
```

## Adding New Tests

When adding new service methods:

1. **Create test method** with descriptive name
2. **Follow AAA pattern** - Arrange, Act, Assert
3. **Mock all dependencies** - Don't create real dependencies
4. **Test happy path** - Normal successful scenario
5. **Test error paths** - Edge cases and failures
6. **Test validation** - Input validation and constraints

**Template:**
```csharp
[Fact]
public async Task ServiceMethod_Scenario_ExpectedResult()
{
    // Arrange
    // Set up test data and mock expectations
    
    // Act
    var result = await _service.MethodAsync(...);
    
    // Assert
    Assert.True(result.Ok);
    // Additional assertions
}
```

## Continuous Integration

To integrate tests into your CI/CD pipeline:

**GitHub Actions Example:**
```yaml
- name: Run Tests
  run: |
    cd assignment4/backend/MortuaryAssistant.Api.Tests
    dotnet test --verbosity normal --logger "trx"
```

**Azure DevOps Example:**
```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: 'assignment4/backend/MortuaryAssistant.Api.Tests'
    publishTestResults: true
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Tests fail after code changes | Review changes, ensure mocks match current API |
| Mock setup errors | Check method signatures haven't changed |
| Database errors in CaseService tests | Ensure unique in-memory DB name per test |
| Timeouts | Increase test timeout or check async/await |
| Inconsistent test failures | Check for test isolation issues or shared state |

## Best Practices

1. ✅ **Test one thing per test** - Single responsibility
2. ✅ **Use descriptive names** - Test names explain intent
3. ✅ **Isolate tests** - No test should depend on another
4. ✅ **Mock external deps** - Don't test dependencies
5. ✅ **Test both success and failure** - Happy and sad paths
6. ✅ **Keep tests fast** - No real I/O or network calls
7. ✅ **Use AAA pattern** - Clear test structure
8. ✅ **DRY for setup** - Reuse common setup in constructors

## References

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Entity Framework Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)
- [Unit Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## Resources

- Test Project: `assignment4/backend/MortuaryAssistant.Api.Tests/`
- All tests: 37 passing
- Framework: xUnit 2.6.3
- Mocking: Moq 4.20.70
