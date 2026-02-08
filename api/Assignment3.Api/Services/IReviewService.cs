using Assignment3.Api.Dtos;

namespace Assignment3.Api.Services;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetLatestAsync(int take);
    Task<ReviewDto> CreateAsync(CreateReviewRequest request);
}
