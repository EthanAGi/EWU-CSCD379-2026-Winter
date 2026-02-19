using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Models;

namespace MortuaryAssistant.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<CaseFile> CaseFiles => Set<CaseFile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CaseFile>()
            .HasIndex(c => c.CaseNumber)
            .IsUnique();
    }
}
