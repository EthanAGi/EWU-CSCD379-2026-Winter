using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Assignment3.Api.Controllers;
using Assignment3.Api.Services;
using Assignment3.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

public class ReviewsControllerTests
{
    [Fact]
    public async Task Get_ReturnsOkResultWithReviews()
    {
        // Arrange
        var mockService = new Mock<IReviewService>();
        var expectedReviews = new List<ReviewDto>
        {
            new ReviewDto { Id = 1, PlayerName = "Player1", Body = "Good", Rating = 4, CreatedAtUtc = DateTime.UtcNow },
            new ReviewDto { Id = 2, PlayerName = "Player2", Body = "Great", Rating = 5, CreatedAtUtc = DateTime.UtcNow }
        };
        mockService.Setup(s => s.GetLatestAsync(It.IsAny<int>())).ReturnsAsync(expectedReviews);
        var controller = new ReviewsController(mockService.Object);

        // Act
        var result = await controller.Get(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReviews = Assert.IsAssignableFrom<IReadOnlyList<ReviewDto>>(okResult.Value);
        Assert.Equal(2, returnedReviews.Count);
        mockService.Verify(s => s.GetLatestAsync(10), Times.Once);
    }

    [Fact]
    public async Task Post_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<IReviewService>();
        var request = new CreateReviewRequest { PlayerName = "TestPlayer", Body = "Excellent!", Rating = 5 };
        var createdReview = new ReviewDto { Id = 1, PlayerName = "TestPlayer", Body = "Excellent!", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        mockService.Setup(s => s.CreateAsync(request)).ReturnsAsync(createdReview);
        var controller = new ReviewsController(mockService.Object);

        // Act
        var result = await controller.Post(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ReviewsController.Get), createdResult.ActionName);
        var returnedReview = Assert.IsType<ReviewDto>(createdResult.Value);
        Assert.Equal("TestPlayer", returnedReview.PlayerName);
    }

    [Fact]
    public async Task Post_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IReviewService>();
        var request = new CreateReviewRequest { PlayerName = "", Body = "No name", Rating = 3 };
        mockService.Setup(s => s.CreateAsync(request)).ThrowsAsync(new ArgumentException("PlayerName is required."));
        var controller = new ReviewsController(mockService.Object);

        // Act
        var result = await controller.Post(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badResult.Value);
    }
}