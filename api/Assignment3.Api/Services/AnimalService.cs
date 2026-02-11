using Assignment3.Api.Data;
using Assignment3.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment3.Api.Services;

public class AnimalService : IAnimalService
{
    private readonly AppDbContext _db;

    public AnimalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AnimalTemplate>> GetTemplatesAsync()
    {
        return await _db.AnimalTemplates
            .OrderBy(t => t.Kind)
            .ToListAsync();
    }

    public async Task<List<PlayerAnimal>> GetPlayerAnimalsAsync(string playerId)
    {
        // You can optionally validate playerId, but leaving it permissive is fine.
        return await _db.PlayerAnimals
            .Where(a => a.OwnerPlayerId == playerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<PlayerAnimal> ClaimAsync(string ownerPlayerId, string ownerName, string kindRaw, string? name)
    {
        // Validation -> 400
        if (string.IsNullOrWhiteSpace(ownerPlayerId))
            throw new ArgumentException("OwnerPlayerId is required.");

        if (string.IsNullOrWhiteSpace(ownerName))
            throw new ArgumentException("OwnerName is required.");

        if (string.IsNullOrWhiteSpace(kindRaw))
            throw new ArgumentException("Kind is required.");

        var kind = kindRaw.Trim().ToLowerInvariant();

        // Template must exist -> 404
        var template = await _db.AnimalTemplates.FirstOrDefaultAsync(t => t.Kind == kind);
        if (template is null)
            throw new KeyNotFoundException($"No template for kind '{kind}'");

        // Rule: only one of each kind per player -> 409
        var alreadyOwns = await _db.PlayerAnimals.AnyAsync(a =>
            a.OwnerPlayerId == ownerPlayerId && a.Kind == kind);

        if (alreadyOwns)
            throw new InvalidOperationException($"Player already owns '{kind}'");

        var displayKind = char.ToUpper(kind[0]) + kind.Substring(1);

        var finalName =
            !string.IsNullOrWhiteSpace(name)
                ? name.Trim()
                : $"{ownerName}'s {displayKind}";

        var pa = new PlayerAnimal
        {
            OwnerPlayerId = ownerPlayerId,
            OwnerName = ownerName,
            Kind = kind,
            Name = finalName,

            TemplateId = template.Id,

            Attack = template.Attack,
            Defense = template.Defense,
            Affection = template.Affection,
            Level = template.Level,
            HpMax = template.HpMax,
            HpCurrent = template.HpMax,

            CreatedAt = DateTime.UtcNow,
        };

        _db.PlayerAnimals.Add(pa);
        await _db.SaveChangesAsync();

        return pa;
    }
}
