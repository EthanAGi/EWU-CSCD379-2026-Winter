using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assignment3.Api.Data;
using Assignment3.Api.Models;
using Assignment3.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class AnimalServiceTests
{
    private static AppDbContext GetTestDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsTemplatesSortedByKind()
    {
        // Arrange
        using var db = GetTestDbContext();

        db.AnimalTemplates.AddRange(
            new AnimalTemplate { Kind = "owl", Attack = 7, Defense = 6, Affection = 2, Level = 1, HpMax = 36 },
            new AnimalTemplate { Kind = "cat", Attack = 6, Defense = 3, Affection = 4, Level = 1, HpMax = 28 },
            new AnimalTemplate { Kind = "dog", Attack = 5, Defense = 4, Affection = 3, Level = 1, HpMax = 30 }
        );
        await db.SaveChangesAsync();

        var service = new AnimalService(db);

        // Act
        var templates = await service.GetTemplatesAsync();

        // Assert
        Assert.Equal(3, templates.Count);

        var kinds = templates.Select(t => t.Kind).ToArray();
        Assert.Equal(new[] { "cat", "dog", "owl" }, kinds);
    }

    [Fact]
    public async Task GetPlayerAnimalsAsync_ReturnsOnlyThatPlayersAnimals_OrderedByCreatedAtDesc()
    {
        // Arrange
        using var db = GetTestDbContext();

        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var older = t0.AddMinutes(1);
        var newer = t0.AddMinutes(2);

        db.PlayerAnimals.AddRange(
            new PlayerAnimal { OwnerPlayerId = "p1", OwnerName = "P1", Kind = "cat", Name = "Old", CreatedAt = older },
            new PlayerAnimal { OwnerPlayerId = "p1", OwnerName = "P1", Kind = "dog", Name = "New", CreatedAt = newer },
            new PlayerAnimal { OwnerPlayerId = "p2", OwnerName = "P2", Kind = "owl", Name = "Other", CreatedAt = newer }
        );
        await db.SaveChangesAsync();

        var service = new AnimalService(db);

        // Act
        var p1Animals = await service.GetPlayerAnimalsAsync("p1");

        // Assert
        Assert.Equal(2, p1Animals.Count);
        Assert.Equal("New", p1Animals[0].Name); // newest first
        Assert.Equal("Old", p1Animals[1].Name);

        Assert.All(p1Animals, a => Assert.Equal("p1", a.OwnerPlayerId));
    }

    [Fact]
    public async Task ClaimAsync_WithMissingFields_ThrowsArgumentException()
    {
        using var db = GetTestDbContext();
        var service = new AnimalService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ClaimAsync("", "Name", "cat", null));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ClaimAsync("p1", "   ", "cat", null));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ClaimAsync("p1", "Name", "   ", null));
    }

    [Fact]
    public async Task ClaimAsync_WithUnknownTemplate_ThrowsKeyNotFoundException()
    {
        using var db = GetTestDbContext();
        var service = new AnimalService(db);

        // no templates seeded -> should 404-style exception
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ClaimAsync("p1", "Name", "cat", null));
    }

    [Fact]
    public async Task ClaimAsync_CreatesPlayerAnimal_UsesTemplateStats_AndDefaultName()
    {
        // Arrange
        using var db = GetTestDbContext();

        db.AnimalTemplates.Add(new AnimalTemplate
        {
            // NOTE: With InMemory DB, Id can be generated or respected depending on your EF config.
            // It's okay to set it here since your service reads template.Id back into TemplateId.
            Id = 123,
            Kind = "cat",
            Attack = 6,
            Defense = 3,
            Affection = 4,
            Level = 1,
            HpMax = 28
        });
        await db.SaveChangesAsync();

        var service = new AnimalService(db);

        // Act
        var created = await service.ClaimAsync("p1", "Ethan", " CAT ", null);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("p1", created.OwnerPlayerId);
        Assert.Equal("Ethan", created.OwnerName);
        Assert.Equal("cat", created.Kind);                 // normalized
        Assert.Equal("Ethan's Cat", created.Name);         // default name
        Assert.Equal(123, created.TemplateId);

        Assert.Equal(6, created.Attack);
        Assert.Equal(3, created.Defense);
        Assert.Equal(4, created.Affection);
        Assert.Equal(1, created.Level);
        Assert.Equal(28, created.HpMax);
        Assert.Equal(28, created.HpCurrent);

        // actually saved
        var saved = await db.PlayerAnimals.FirstOrDefaultAsync(a => a.OwnerPlayerId == "p1" && a.Kind == "cat");
        Assert.NotNull(saved);
        Assert.True(saved!.Id > 0);
    }

    [Fact]
    public async Task ClaimAsync_WhenPlayerAlreadyOwnsKind_ThrowsInvalidOperationException()
    {
        // Arrange
        using var db = GetTestDbContext();

        db.AnimalTemplates.Add(new AnimalTemplate
        {
            Id = 1,
            Kind = "dog",
            Attack = 5,
            Defense = 4,
            Affection = 3,
            Level = 1,
            HpMax = 30
        });
        await db.SaveChangesAsync();

        db.PlayerAnimals.Add(new PlayerAnimal
        {
            OwnerPlayerId = "p1",
            OwnerName = "Ethan",
            Kind = "dog",
            Name = "Existing Dog",
            TemplateId = 1,
            Attack = 5,
            Defense = 4,
            Affection = 3,
            Level = 1,
            HpMax = 30,
            HpCurrent = 30,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var service = new AnimalService(db);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ClaimAsync("p1", "Ethan", "dog", "New Dog"));
    }
}
