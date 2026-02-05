using Microsoft.EntityFrameworkCore;
using Assignment3.Api.Models;

namespace Assignment3.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Score> Scores => Set<Score>();
}
