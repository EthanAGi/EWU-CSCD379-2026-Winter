using Assignment3.Api.Dtos;
using Assignment3.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Assignment3.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    // GET /api/reviews?take=50
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> Get([FromQuery] int take = 50)
    {
        var result = await _reviews.GetLatestAsync(take);
        return Ok(result);
    }

    // POST /api/reviews
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Post([FromBody] CreateReviewRequest request)
    {
        try
        {
            var created = await _reviews.CreateAsync(request);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
