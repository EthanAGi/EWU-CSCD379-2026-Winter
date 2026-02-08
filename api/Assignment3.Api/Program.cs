using Assignment3.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ✅ CORS (allow Nuxt dev server to call this API on localhost)
var corsPolicyName = "NuxtDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();

        // If you ever use cookies/auth between Nuxt and API, use this instead:
        // policy.WithOrigins("http://localhost:3000")
        //       .AllowAnyHeader()
        //       .AllowAnyMethod()
        //       .AllowCredentials();
    });
});

// Entity Framework + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ TEMP: Definitive DB connectivity test (prints to Log Stream on startup)
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
        Console.WriteLine(ex.ToString()); // full details for Azure Log Stream
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ✅ Enable CORS (must be before MapControllers; before auth/authorization if you add them later)
app.UseCors(corsPolicyName);

// Map attribute-routed controllers (e.g., /api/scores)
app.MapControllers();

app.Run();
