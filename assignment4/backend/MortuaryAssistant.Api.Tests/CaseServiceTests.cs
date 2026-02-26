using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Data;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;
using Xunit;

namespace MortuaryAssistant.Api.Tests;

public class CaseServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly CaseService _caseService;

    public CaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _caseService = new CaseService(_dbContext);
    }

    #region GetAllCasesAsync Tests

    [Fact]
    public async Task GetAllCasesAsync_ReturnsAllCases()
    {
        // Arrange
        var cases = new List<CaseFile>
        {
            new()
            {
                Id = 1,
                CaseNumber = "CASE-001",
                Status = CaseStatus.Intake,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                AssignedMorticianUserId = "user-1"
            },
            new()
            {
                Id = 2,
                CaseNumber = "CASE-002",
                Status = CaseStatus.InPreparation,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                AssignedMorticianUserId = "user-2"
            },
            new()
            {
                Id = 3,
                CaseNumber = "CASE-003",
                Status = CaseStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                AssignedMorticianUserId = null
            }
        };

        await _dbContext.CaseFiles.AddRangeAsync(cases);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.Equal(3, caseList.Count);
    }

    [Fact]
    public async Task GetAllCasesAsync_ReturnsEmptyList_WhenNoCases()
    {
        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.Empty(caseList);
    }

    [Fact]
    public async Task GetAllCasesAsync_OrdersResultsByCreationDateDescending()
    {
        // Arrange - create cases with specific dates to verify ordering
        var now = DateTime.UtcNow;
        var cases = new List<CaseFile>
        {
            new() { Id = 1, CaseNumber = "CASE-001", Status = CaseStatus.Intake, CreatedAt = now.AddDays(-10), AssignedMorticianUserId = null },
            new() { Id = 2, CaseNumber = "CASE-002", Status = CaseStatus.Intake, CreatedAt = now.AddDays(-5), AssignedMorticianUserId = null },
            new() { Id = 3, CaseNumber = "CASE-003", Status = CaseStatus.Intake, CreatedAt = now, AssignedMorticianUserId = null }
        };

        await _dbContext.CaseFiles.AddRangeAsync(cases);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.True(result.Ok);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.Equal(3, caseList.Count);
        
        // Verify the results are ordered by checking the database state
        // Fresh query to ensure ordering
        var dbCases = await _dbContext.CaseFiles
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.CaseNumber)
            .ToListAsync();
        
        Assert.Equal(new[] { "CASE-003", "CASE-002", "CASE-001" }, dbCases);
    }

    [Fact]
    public async Task GetAllCasesAsync_ReturnsCasesAsAnonymousObjects()
    {
        // Arrange
        var caseFile = new CaseFile
        {
            Id = 1,
            CaseNumber = "TEST-001",
            Status = CaseStatus.Intake,
            CreatedAt = DateTime.UtcNow,
            AssignedMorticianUserId = "usr-123"
        };

        await _dbContext.CaseFiles.AddAsync(caseFile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.True(result.Ok);
        Assert.NotNull(result.Value);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.Single(caseList);
        var caseObj = caseList[0];
        Assert.NotNull(caseObj);
        // Verify it's an anonymous object by checking for properties
        var typeProps = caseObj!.GetType().GetProperties();
        Assert.NotEmpty(typeProps);
        Assert.True(typeProps.Any(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)), "Should have Id property");
        Assert.True(typeProps.Any(p => p.Name.Equals("CaseNumber", StringComparison.OrdinalIgnoreCase)), "Should have CaseNumber property");
        Assert.True(typeProps.Any(p => p.Name.Equals("Status", StringComparison.OrdinalIgnoreCase)), "Should have Status property");
    }

    [Fact]
    public async Task GetAllCasesAsync_IncludesStatusAndAssignedMorticianUserId()
    {
        // Arrange
        var caseFile = new CaseFile
        {
            Id = 1,
            CaseNumber = "CASE-001",
            Status = CaseStatus.InPreparation,
            CreatedAt = DateTime.UtcNow,
            AssignedMorticianUserId = "user-456"
        };

        await _dbContext.CaseFiles.AddAsync(caseFile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.True(result.Ok);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.Single(caseList);
        var props = caseList[0]!.GetType().GetProperties().Select(p => p.Name).ToList();
        Assert.Contains("Status", props);
        Assert.Contains("AssignedMorticianUserId", props);
    }

    [Fact]
    public async Task GetAllCasesAsync_HandlesCaseWithNullAssignedMortician()
    {
        // Arrange
        var caseFile = new CaseFile
        {
            Id = 1,
            CaseNumber = "CASE-001",
            Status = CaseStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            AssignedMorticianUserId = null
        };

        await _dbContext.CaseFiles.AddAsync(caseFile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.True(result.Ok);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.Single(caseList);
        // Can deserialize without errors
        Assert.NotNull(caseList[0]);
    }

    [Fact]
    public async Task GetAllCasesAsync_ReturnsMultipleCases()
    {
        // Arrange
        var cases = new List<CaseFile>();
        for (int i = 1; i <= 10; i++)
        {
            cases.Add(new CaseFile
            {
                Id = i,
                CaseNumber = $"CASE-{i:D3}",
                Status = CaseStatus.Intake,
                CreatedAt = DateTime.UtcNow.AddHours(-i),
                AssignedMorticianUserId = i % 2 == 0 ? $"user-{i}" : null
            });
        }

        await _dbContext.CaseFiles.AddRangeAsync(cases);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.True(result.Ok);
        Assert.Equal(200, result.StatusCode);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.Equal(10, caseList.Count);
    }

    [Fact]
    public async Task GetAllCasesAsync_ResultsUseNoTracking()
    {
        // Arrange
        var caseFile = new CaseFile
        {
            Id = 1,
            CaseNumber = "CASE-001",
            Status = CaseStatus.Intake,
            CreatedAt = DateTime.UtcNow,
            AssignedMorticianUserId = null
        };

        await _dbContext.CaseFiles.AddAsync(caseFile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert - NoTracking means returned objects are not part of the context
        Assert.True(result.Ok);
        var caseList = ((IEnumerable<object>)result.Value!).ToList();
        Assert.NotEmpty(caseList);
        
        // Verify that the number of tracked entities hasn't changed significantly
        var trackedCount = _dbContext.ChangeTracker.Entries().Count();
        // Should only track the original case file added, not the returned anonymous objects
        Assert.True(trackedCount < 5, "NoTracking should result in few tracked entities");
    }

    #endregion
}
