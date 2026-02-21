using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Data;
using MortuaryAssistant.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI + ✅ JWT support in Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MortuaryAssistant.Api", Version = "v1" });

    // ✅ Add the "Authorize" button for Bearer tokens
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

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// ✅ JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT settings missing. Add Jwt:Key, Jwt:Issuer, Jwt:Audience in appsettings.json");
}

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
            ValidAudience = jwtAudience,
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

    // Ensure Admin role
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
 * ✅ DEV Seed: workflow templates, equipment, sample cases,
 *    decedents, tasks, notes (DEV ONLY)
 * ------------------------------------------------------- */
static void SeedDevData(AppDbContext db)
{
    // 1) Workflow templates
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

    // 2) Equipment
    if (!db.Equipment.Any())
    {
        db.Equipment.AddRange(
            new Equipment { Name = "Embalming Table", SerialNumber = "ET-100", Status = EquipmentStatus.Available, Location = "Prep Room", IsActive = true },
            new Equipment { Name = "Lift", SerialNumber = "LIFT-22", Status = EquipmentStatus.Available, Location = "Garage", IsActive = true },
            new Equipment { Name = "Cosmetics Kit", SerialNumber = "CK-09", Status = EquipmentStatus.Available, Location = "Prep Room", IsActive = true }
        );
        db.SaveChanges();
    }

    // 3) Sample cases + decedents
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

    // 4) Instantiate workflow tasks for each case (only if missing for that case)
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

    // 5) Sample notes
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
 * ✅ Startup scope
 * ------------------------------------------------------- */
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Dev-only migrate + seed sample data
    if (app.Environment.IsDevelopment())
    {
        var db = services.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        SeedDevData(db);
    }

    // Always seed roles
    await SeedRolesAsync(services);

    // Bootstrap initial admin (config-controlled)
    await SeedAdminUserAsync(services, builder.Configuration);
}

// Swagger only in dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANT: auth must be before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();