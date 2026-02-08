using Assignment3.Api.Data;
using Assignment3.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment3.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AnimalsController(AppDbContext db) => _db = db;

    // ✅ matches the Nuxt page: GET /api/animals/templates
    [HttpGet("templates")]
    public async Task<List<AnimalTemplate>> GetTemplates() =>
        await _db.AnimalTemplates.OrderBy(t => t.Kind).ToListAsync();

    // ✅ useful endpoint for later (load a player's owned animals)
    [HttpGet("player/{playerId}")]
    public async Task<List<PlayerAnimal>> GetPlayerAnimals(string playerId) =>
        await _db.PlayerAnimals
            .Where(a => a.OwnerPlayerId == playerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    // ✅ matches the Nuxt page body:
    // POST /api/animals/claim
    // { ownerPlayerId, ownerName, kind, name }
    public record ClaimAnimalRequest(string OwnerPlayerId, string OwnerName, string Kind, string? Name);

    [HttpPost("claim")]
    public async Task<ActionResult<PlayerAnimal>> Claim([FromBody] ClaimAnimalRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.OwnerPlayerId)) return BadRequest("OwnerPlayerId is required.");
        if (string.IsNullOrWhiteSpace(req.OwnerName)) return BadRequest("OwnerName is required.");
        if (string.IsNullOrWhiteSpace(req.Kind)) return BadRequest("Kind is required.");

        var kind = req.Kind.Trim().ToLower();

        var template = await _db.AnimalTemplates.FirstOrDefaultAsync(t => t.Kind == kind);
        if (template is null) return NotFound($"No template for kind '{kind}'");

        // Optional rule: only one of each kind per player (matches your TS logic)
        var alreadyOwns = await _db.PlayerAnimals.AnyAsync(a =>
            a.OwnerPlayerId == req.OwnerPlayerId && a.Kind == kind);

        if (alreadyOwns) return Conflict($"Player already owns '{kind}'");

        var displayKind = char.ToUpper(kind[0]) + kind.Substring(1);

        // If the client provided a name (e.g. "Ethan's Dog") use it; otherwise default.
        var finalName = !string.IsNullOrWhiteSpace(req.Name)
            ? req.Name!.Trim()
            : $"{req.OwnerName}'s {displayKind}";

        var pa = new PlayerAnimal
        {
            OwnerPlayerId = req.OwnerPlayerId,
            OwnerName = req.OwnerName,
            Kind = kind,
            Name = finalName,

            TemplateId = template.Id,

            Attack = template.Attack,
            Defense = template.Defense,
            Affection = template.Affection,
            Level = template.Level,
            HpMax = template.HpMax,
            HpCurrent = template.HpMax,
            // CreatedAt should already default in your model; keeping it safe:
            CreatedAt = DateTime.UtcNow,
        };

        _db.PlayerAnimals.Add(pa);
        await _db.SaveChangesAsync();

        return Ok(pa);
    }
}
