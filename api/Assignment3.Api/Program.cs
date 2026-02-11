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
