using Microsoft.EntityFrameworkCore;
using Assignment3.Api.Models;

namespace Assignment3.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ✅ Replace Scores with these two tables:
    public DbSet<AnimalTemplate> AnimalTemplates => Set<AnimalTemplate>();
    public DbSet<PlayerAnimal> PlayerAnimals => Set<PlayerAnimal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ✅ Ensure only one template row per kind (dog/cat/etc)
        modelBuilder.Entity<AnimalTemplate>()
            .HasIndex(t => t.Kind)
            .IsUnique();

        // ✅ Seed one base version of every animal (matches your baseStats() in TS)
        modelBuilder.Entity<AnimalTemplate>().HasData(
            new AnimalTemplate { Id = 1, Kind = "dog", Attack = 5, Defense = 4, Affection = 3, Level = 1, HpMax = 30 },
            new AnimalTemplate { Id = 2, Kind = "cat", Attack = 6, Defense = 3, Affection = 4, Level = 1, HpMax = 28 },
            new AnimalTemplate { Id = 3, Kind = "hamster", Attack = 4, Defense = 5, Affection = 2, Level = 1, HpMax = 32 },
            new AnimalTemplate { Id = 4, Kind = "fox", Attack = 8, Defense = 5, Affection = 2, Level = 1, HpMax = 34 },
            new AnimalTemplate { Id = 5, Kind = "owl", Attack = 7, Defense = 6, Affection = 2, Level = 1, HpMax = 36 },
            new AnimalTemplate { Id = 6, Kind = "boar", Attack = 6, Defense = 8, Affection = 1, Level = 1, HpMax = 40 }
        );
    }
}
