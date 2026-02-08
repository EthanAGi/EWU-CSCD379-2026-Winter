using Assignment3.Api.Data;
using Assignment3.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ✅ CORS (Nuxt local + Azure deployed site)
var corsPolicyName = "NuxtDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://assignment3-b6dfeygfb0bgfgbr.eastus2-01.azurewebsites.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();

        // If you ever switch to cookies/auth, use:
        // .AllowCredentials();
    });
});

// ✅ Register business logic services
builder.Services.AddScoped<IReviewService, ReviewService>();

// ✅ Entity Framework + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// TESTTESTTEST
var app = builder.Build();

// ✅ TEMP: Definitive DB connectivity test (safe for Azure logs)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.OpenConnection();
        Console.WriteLine("✅ DATABASE CONNECTION SUCCESSFUL");
        db.Database.CloseConnection();
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ DATABASE CONNECTION FAILED");
        Console.WriteLine(ex.ToString());
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ✅ Enable CORS (must be BEFORE MapControllers)
app.UseCors(corsPolicyName);

// Map attribute-routed controllers (e.g., /api/reviews)
app.MapControllers();

app.Run();
