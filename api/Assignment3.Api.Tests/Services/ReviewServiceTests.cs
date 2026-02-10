using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Assignment3.Api.Services;
using Assignment3.Api.Data;
using Assignment3.Api.Models;
using Assignment3.Api.Dtos;
using Microsoft.EntityFrameworkCore;

public class ReviewServiceTests
{
    private AppDbContext GetTestDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsReviewDto()
    {
        // Arrange
        var db = GetTestDbContext();
        var service = new ReviewService(db);
        var request = new CreateReviewRequest
        {
            PlayerName = "TestPlayer",
            Body = "Great game!",
            Rating = 5
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestPlayer", result.PlayerName);
        Assert.Equal("Great game!", result.Body);
        Assert.Equal(5, result.Rating);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyPlayerName_ThrowsException()
    {
        // Arrange
        var db = GetTestDbContext();
        var service = new ReviewService(db);
        var request = new CreateReviewRequest
        {
            PlayerName = "   ",
            Body = "Great game!",
            Rating = 5
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsReviewsOrderedByDateDescending()
    {
        // Arrange
        var db = GetTestDbContext();
        var service = new ReviewService(db);
        var oldReview = new Review { PlayerName = "Player1", Body = "Old", Rating = 3, CreatedAtUtc = DateTime.UtcNow.AddDays(-1) };
        var newReview = new Review { PlayerName = "Player2", Body = "New", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        
        db.Reviews.Add(oldReview);
        db.Reviews.Add(newReview);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetLatestAsync(10);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Player2", result[0].PlayerName);
        Assert.Equal("Player1", result[1].PlayerName);
    }
}