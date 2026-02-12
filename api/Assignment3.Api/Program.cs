using Assignment3.Api.Data;
using Assignment3.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ✅ CORS (Nuxt local dev + Azure Static Web App)
var corsPolicyName = "ClientCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://green-forest-00c371e0f.2.azurestaticapps.net"
                // If your SWA uses a staging URL during PR deploys, add it here too.
                // Example format: https://<random>-green-forest-00c371e0f.2.azurestaticapps.net
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ✅ Register business logic services
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAnimalService, AnimalService>();

// ✅ Entity Framework + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

/**
 * ✅ IMPORTANT for Azure Linux App Service:
 * Bind to the port Azure provides (usually 8080 via PORT).
 * If you don't, the container can "start" but never becomes reachable.
 */
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// ✅ Always enable Swagger so you can verify endpoints in Azure
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirection is fine (Azure terminates TLS at the front door)
app.UseHttpsRedirection();

// ✅ Enable CORS (must be BEFORE MapControllers)
app.UseCors(corsPolicyName);

// Map attribute-routed controllers (e.g., /api/reviews)
app.MapControllers();

// Optional: makes the root not 404, helpful for quick checks
app.MapGet("/", () => Results.Ok("Assignment3 API is running"));

app.Run();
