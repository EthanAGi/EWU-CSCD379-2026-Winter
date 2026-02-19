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

    // ✅ public page requirement: shows DB data to anyone
    [HttpGet("cases")]
    public async Task<IActionResult> PublicCases()
    {
        var data = await _db.CaseFiles
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.CaseNumber, c.Status, c.CreatedAt })
            .ToListAsync();

        return Ok(data);
    }
}
