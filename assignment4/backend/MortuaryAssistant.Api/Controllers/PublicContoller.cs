using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Data;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _db;
    public PublicController(AppDbContext db) => _db = db;

    // ✅ Public: shows DB data to anyone (including not logged-in users)
    // Optional but explicit:
    [AllowAnonymous]
    [HttpGet("cases")]
    public async Task<IActionResult> PublicCases()
    {
        var data = await _db.CaseFiles
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.CaseNumber,
                Status = c.Status.ToString(),   // nicer for frontend than enum int
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(data);
    }
}