using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Data;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers (this automatically includes ALL controllers in your project)
builder.Services.AddControllers();

/* -------------------------------------------------------
 * ✅ CORS (so Nuxt Static Web App can call this API)
 *
 * Azure App Service -> Configuration -> Application settings:
 *   CORS_ALLOWED_ORIGINS = https://your-staticapp.azurestaticapps.net;https://your-domain.com
 * ------------------------------------------------------- */
var allowedOrigins = new List<string>
{
    "http://localhost:3000",
    "http://localhost:5173"
};

var corsFromConfig = builder.Configuration["CORS_ALLOWED_ORIGINS"];
if (!string.IsNullOrWhiteSpace(corsFromConfig))
{
    allowedOrigins.AddRange(
        corsFromConfig.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    );
}

allowedOrigins = allowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger/OpenAPI + ✅ JWT support in Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MortuaryAssistant.Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste ONLY the JWT token here (no 'Bearer ' prefix)."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ✅ Database: fail-fast with a clear error message if missing in Azure
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'DefaultConnection'. " +
        "In Azure App Service -> Configuration -> Connection strings, add DefaultConnection (Type: SQLAzure).");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity + Roles
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ✅ Register YOUR app services (DI)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ICaseService, CaseService>();

// ✅ JWT Auth (fail-fast, clear message)
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT settings missing. In Azure App Service -> Configuration -> Application settings add:\n" +
        "  Jwt__Key, Jwt__Issuer, Jwt__Audience");
}

/*
 * ✅ IMPORTANT FIX:
 * Your AuthService.cs (token minting) previously used audience = jwtIssuer.
 * But validation expects jwtAudience.
 *
 * To prevent 401s when tokens were minted with aud=issuer, we accept BOTH:
 *  - Jwt:Audience
 *  - Jwt:Issuer
 *
 * You should STILL fix AuthService.cs to mint audience = Jwt:Audience.
 */
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwtIssuer,

            // ✅ Accept both configured audience and issuer as "valid audiences"
            // (handles older tokens minted with aud=issuer)
            ValidAudiences = new[] { jwtAudience, jwtIssuer },

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

/* -------------------------------------------------------
 * ✅ Seed Roles (ALL environments)
 * ------------------------------------------------------- */
static async Task SeedRolesAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames = { Roles.Admin, Roles.Mortician };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

/* -------------------------------------------------------
 * ✅ Bootstrap FIRST Admin (controlled via config)
 * ------------------------------------------------------- */
static async Task SeedAdminUserAsync(IServiceProvider services, IConfiguration config)
{
    var enabled = config.GetValue<bool>("AdminSeed:Enabled");
    if (!enabled) return;

    var email = config["AdminSeed:Email"];
    var password = config["AdminSeed:Password"];
    var displayName = config["AdminSeed:DisplayName"] ?? "Admin";

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return;

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var msg = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"AdminSeed user create failed: {msg}");
        }
    }

    if (!await userManager.IsInRoleAsync(user, Roles.Admin))
    {
        var addRole = await userManager.AddToRoleAsync(user, Roles.Admin);
        if (!addRole.Succeeded)
        {
            var msg = string.Join("; ", addRole.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"AdminSeed add role failed: {msg}");
        }
    }
}

/* -------------------------------------------------------
 * ✅ DEV/Sample data seeding (idempotent)
 * ------------------------------------------------------- */
static void SeedDevData(AppDbContext db)
{
    if (!db.WorkflowStepTemplates.Any())
    {
        db.WorkflowStepTemplates.AddRange(
            new WorkflowStepTemplate { Name = "Intake paperwork", Description = "Collect and verify initial paperwork", SortOrder = 1, IsActive = true },
            new WorkflowStepTemplate { Name = "Tag + storage", Description = "Assign tag number and storage location", SortOrder = 2, IsActive = true },
            new WorkflowStepTemplate { Name = "Preparation", Description = "Embalming / preparation process", SortOrder = 3, IsActive = true },
            new WorkflowStepTemplate { Name = "Cosmetics + dressing", Description = "Cosmetics, dressing, presentation", SortOrder = 4, IsActive = true },
            new WorkflowStepTemplate { Name = "Ready for viewing", Description = "Final QA check before viewing", SortOrder = 5, IsActive = true },
            new WorkflowStepTemplate { Name = "Release / completion", Description = "Release remains / finalize case", SortOrder = 6, IsActive = true }
        );
        db.SaveChanges();
    }

    if (!db.Equipment.Any())
    {
        db.Equipment.AddRange(
            new Equipment { Name = "Embalming Table", SerialNumber = "ET-100", Status = EquipmentStatus.Available, Location = "Prep Room", IsActive = true },
            new Equipment { Name = "Lift", SerialNumber = "LIFT-22", Status = EquipmentStatus.Available, Location = "Garage", IsActive = true },
            new Equipment { Name = "Cosmetics Kit", SerialNumber = "CK-09", Status = EquipmentStatus.Available, Location = "Prep Room", IsActive = true }
        );
        db.SaveChanges();
    }

    if (!db.CaseFiles.Any())
    {
        var c1 = new CaseFile
        {
            CaseNumber = "CASE-1001",
            Status = CaseStatus.Intake,
            NextOfKinName = "Jordan Smith",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Decedent = new Decedent
            {
                FirstName = "Alex",
                LastName = "Morgan",
                DateOfDeath = DateTime.UtcNow.AddDays(-3),
                TagNumber = "TAG-1001",
                StorageLocation = "Cooler A - Shelf 2"
            }
        };

        var c2 = new CaseFile
        {
            CaseNumber = "CASE-1002",
            Status = CaseStatus.InPreparation,
            NextOfKinName = "Taylor Johnson",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Decedent = new Decedent
            {
                FirstName = "Sam",
                LastName = "Reed",
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                TagNumber = "TAG-1002",
                StorageLocation = "Cooler A - Shelf 3"
            }
        };

        var c3 = new CaseFile
        {
            CaseNumber = "CASE-1003",
            Status = CaseStatus.ReadyForViewing,
            NextOfKinName = "Casey Lee",
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            Decedent = new Decedent
            {
                FirstName = "Jamie",
                LastName = "Kim",
                DateOfDeath = DateTime.UtcNow.AddDays(-1),
                TagNumber = "TAG-1003",
                StorageLocation = "Cooler B - Shelf 1"
            }
        };

        db.CaseFiles.AddRange(c1, c2, c3);
        db.SaveChanges();
    }

    var templates = db.WorkflowStepTemplates
        .Where(t => t.IsActive)
        .OrderBy(t => t.SortOrder)
        .ToList();

    var caseIds = db.CaseFiles.Select(c => c.Id).ToList();

    foreach (var caseId in caseIds)
    {
        if (db.CaseTasks.Any(t => t.CaseFileId == caseId))
            continue;

        foreach (var step in templates)
        {
            db.CaseTasks.Add(new CaseTask
            {
                CaseFileId = caseId,
                WorkflowStepTemplateId = step.Id,
                Status = CaseTaskStatus.Todo,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
    db.SaveChanges();

    if (!db.CaseNotes.Any())
    {
        var firstCaseId = db.CaseFiles.OrderBy(c => c.Id).Select(c => c.Id).First();

        db.CaseNotes.AddRange(
            new CaseNote { CaseFileId = firstCaseId, Text = "Initial intake completed. Awaiting family paperwork.", CreatedAt = DateTime.UtcNow.AddHours(-4) },
            new CaseNote { CaseFileId = firstCaseId, Text = "Storage location confirmed. Tag applied.", CreatedAt = DateTime.UtcNow.AddHours(-2) }
        );

        db.SaveChanges();
    }
}

/* -------------------------------------------------------
 * ✅ Startup scope:
 * - Migrate DB
 * - Seed roles
 * - Optional: seed first admin
 * - Optional: seed sample data if toggled
 * ------------------------------------------------------- */
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    db.Database.Migrate();

    // ✅ Roles FIRST, then admin (admin role must exist)
    await SeedRolesAsync(services);
    await SeedAdminUserAsync(services, builder.Configuration);

    var seedEnabled = builder.Configuration.GetValue<bool>("SeedSampleData:Enabled");
    if (seedEnabled && !db.CaseFiles.Any())
    {
        SeedDevData(db);
    }
}

/* -------------------------------------------------------
 * ✅ Swagger in Azure (optional)
 * Azure App Settings: Swagger__Enabled = true
 * ------------------------------------------------------- */
var swaggerEnabled = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

// This maps ALL controller routes (AuthController, PublicController, CaseFilesController, MorticianController, AdminUsersController, etc.)
app.MapControllers();

app.Run();