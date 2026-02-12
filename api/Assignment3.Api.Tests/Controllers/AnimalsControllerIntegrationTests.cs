using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Assignment3.Api.Data;
using Assignment3.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AnimalsControllerIntegrationTests : IClassFixture<AnimalsControllerIntegrationTests.CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnimalsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTemplates_ReturnsOk_AndArray()
    {
        var resp = await _client.GetAsync("/api/Animals/templates");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var templates = await resp.Content.ReadFromJsonAsync<List<AnimalTemplate>>(JsonOpts());
        Assert.NotNull(templates);
        Assert.NotEmpty(templates!);

        // verify at least one seeded kind is present
        Assert.Contains(templates!, t => t.Kind == "cat" || t.Kind == "dog" || t.Kind == "hamster");
    }

    [Fact]
    public async Task Claim_Then_GetPlayerAnimals_ReturnsCreatedAnimal()
    {
        // Arrange
        var playerId = "p-int-1";

        var req = new ClaimAnimalRequest(
            OwnerPlayerId: playerId,
            OwnerName: "IntegrationTester",
            Kind: "cat",
            Name: "Whiskers"
        );

        // Act: claim
        var claimResp = await _client.PostAsJsonAsync("/api/Animals/claim", req);
        Assert.Equal(HttpStatusCode.OK, claimResp.StatusCode);

        var created = await claimResp.Content.ReadFromJsonAsync<PlayerAnimal>(JsonOpts());
        Assert.NotNull(created);

        Assert.Equal(playerId, created!.OwnerPlayerId);
        Assert.Equal("IntegrationTester", created.OwnerName);
        Assert.Equal("cat", created.Kind);
        Assert.Equal("Whiskers", created.Name);
        Assert.True(created.TemplateId > 0);
        Assert.True(created.HpMax > 0);
        Assert.Equal(created.HpMax, created.HpCurrent);

        // Act: get by player
        var getResp = await _client.GetAsync($"/api/Animals/player/{Uri.EscapeDataString(playerId)}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var list = await getResp.Content.ReadFromJsonAsync<List<PlayerAnimal>>(JsonOpts());
        Assert.NotNull(list);
        Assert.Contains(list!, a => a.OwnerPlayerId == playerId && a.Kind == "cat" && a.Name == "Whiskers");
    }

    [Fact]
    public async Task Claim_WithUnknownKind_Returns404()
    {
        var req = new ClaimAnimalRequest("p-int-2", "IntegrationTester", "doesnotexist", null);

        var resp = await _client.PostAsJsonAsync("/api/Animals/claim", req);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Claim_SameKindTwice_Returns409()
    {
        var playerId = "p-int-3";
        var req = new ClaimAnimalRequest(playerId, "IntegrationTester", "dog", null);

        var first = await _client.PostAsJsonAsync("/api/Animals/claim", req);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/Animals/claim", req);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Claim_MissingOwnerPlayerId_Returns400()
    {
        var req = new ClaimAnimalRequest("   ", "IntegrationTester", "cat", "Name");

        var resp = await _client.PostAsJsonAsync("/api/Animals/claim", req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---- Test host + InMemory DB ----

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with InMemory
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var dbName = "AnimalsIntDb_" + Guid.NewGuid();

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                );

                // Build provider so we can seed templates
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Database.EnsureCreated();
                SeedTemplates(db);
            });
        }

        private static void SeedTemplates(AppDbContext db)
        {
            // idempotent seed
            if (db.AnimalTemplates.Any()) return;

            db.AnimalTemplates.AddRange(
                new AnimalTemplate { Kind = "cat", Attack = 6, Defense = 3, Affection = 4, Level = 1, HpMax = 28 },
                new AnimalTemplate { Kind = "dog", Attack = 5, Defense = 4, Affection = 3, Level = 1, HpMax = 30 },
                new AnimalTemplate { Kind = "hamster", Attack = 4, Defense = 5, Affection = 2, Level = 1, HpMax = 32 }
            );

            db.SaveChanges();
        }
    }

    private static JsonSerializerOptions JsonOpts() => new()
    {
        PropertyNameCaseInsensitive = true
    };

    public record ClaimAnimalRequest(string OwnerPlayerId, string OwnerName, string Kind, string? Name);
}
