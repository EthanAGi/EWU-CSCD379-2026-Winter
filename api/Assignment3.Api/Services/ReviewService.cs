using Assignment3.Api.Data;
using Assignment3.Api.Dtos;
using Assignment3.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment3.Api.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetLatestAsync(int take)
    {
        take = Math.Clamp(take, 1, 200);

        return await _db.Reviews
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(take)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                PlayerName = r.PlayerName,
                Body = r.Body,
                Rating = r.Rating,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<ReviewDto> CreateAsync(CreateReviewRequest request)
    {
        var playerName = (request.PlayerName ?? "").Trim();
        var body = (request.Body ?? "").Trim();

        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("PlayerName is required.");

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.");

        var review = new Review
        {
            PlayerName = playerName,
            Body = body,
            Rating = Math.Clamp(request.Rating, 1, 5),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        return new ReviewDto
        {
            Id = review.Id,
            PlayerName = review.PlayerName,
            Body = review.Body,
            Rating = review.Rating,
            CreatedAtUtc = review.CreatedAtUtc
        };
    }
}
