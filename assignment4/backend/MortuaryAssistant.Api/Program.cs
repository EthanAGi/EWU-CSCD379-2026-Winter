using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Data;
using MortuaryAssistant.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity + Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthorization();

var app = builder.Build();

// ✅ Seed database with sample public cases (DEV ONLY)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Make sure DB/migrations are applied
    db.Database.Migrate();

    if (!db.CaseFiles.Any())
    {
        db.CaseFiles.AddRange(
            new CaseFile
            {
                CaseNumber = "CASE-1001",
                Status = CaseStatus.Intake,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new CaseFile
            {
                CaseNumber = "CASE-1002",
                Status = CaseStatus.InPreparation,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new CaseFile
            {
                CaseNumber = "CASE-1003",
                Status = CaseStatus.ReadyForViewing,
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            }
        );

        db.SaveChanges();
    }
}

// Swagger only in dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();