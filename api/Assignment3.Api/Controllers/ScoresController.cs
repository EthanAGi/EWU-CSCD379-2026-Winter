using Assignment3.Api.Data;
using Assignment3.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment3.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase
{
    private readonly AppDbContext _db;
    public ScoresController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<List<Score>> Get() =>
        await _db.Scores.OrderByDescending(s => s.CreatedAt).ToListAsync();

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Score score)
    {
        _db.Scores.Add(score);
        await _db.SaveChangesAsync();
        return Ok(score);
    }
}
