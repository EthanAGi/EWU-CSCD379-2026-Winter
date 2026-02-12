using Assignment3.Api.Data;
using Assignment3.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
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

// ✅ Helpful error output
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            title = "Server error",
            status = 500,
            detail = ex?.Message,
            type = ex?.GetType().FullName
        });
    });
});

// Swagger enabled everywhere
app.UseSwagger();
app.UseSwaggerUI();

// ✅ Auto-migrate DB (ONLY if provider is relational)
// In integration tests you replace SQL Server with EF InMemory,
// and InMemory is NOT relational, so Migrate() would throw.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.UseHttpsRedirection();

// ✅ Enable CORS BEFORE MapControllers
app.UseCors(corsPolicyName);

app.MapControllers();

app.Run();

// 🔥 REQUIRED FOR INTEGRATION TESTS
public partial class Program { }
