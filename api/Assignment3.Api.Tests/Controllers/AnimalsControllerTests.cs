using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Assignment3.Api.Data;
using Assignment3.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AnimalsControllerTests : IClassFixture<AnimalsControllerTests.CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnimalsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ClaimAnimal_ReturnsCreatedAnimalDto()
    {
        // Arrange
        var req = new ClaimAnimalRequest
        {
            OwnerPlayerId = "player-test-1",
            OwnerName = "TestPlayer",
            Kind = "cat",
            Name = "Test Cat"
        };

        // Act
        var resp = await _client.PostAsJsonAsync("/api/Animals/claim", req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<PlayerAnimalDto>(JsonOpts());
        Assert.NotNull(dto);

        Assert.True(dto!.Id > 0);
        Assert.Equal("player-test-1", dto.OwnerPlayerId);
        Assert.Equal("TestPlayer", dto.OwnerName);
        Assert.Equal("cat", dto.Kind);
        Assert.Equal("Test Cat", dto.Name);
        Assert.True(dto.Attack > 0);
        Assert.True(dto.Defense > 0);
        Assert.True(dto.HpMax > 0);
        Assert.True(dto.HpCurrent > 0);
    }

    [Fact]
    public async Task GetPlayerAnimals_AfterClaim_ReturnsAnimalInList()
    {
        // Arrange
        var playerId = "player-test-2";

        var claimReq = new ClaimAnimalRequest
        {
            OwnerPlayerId = playerId,
            OwnerName = "AnotherPlayer",
            Kind = "dog",
            Name = "Doggo"
        };

        var claimResp = await _client.PostAsJsonAsync("/api/Animals/claim", claimReq);
        claimResp.EnsureSuccessStatusCode();

        // Act
        var getResp = await _client.GetAsync($"/api/Animals/player/{Uri.EscapeDataString(playerId)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var list = await getResp.Content.ReadFromJsonAsync<List<PlayerAnimalDto>>(JsonOpts());
        Assert.NotNull(list);
        Assert.NotEmpty(list!);

        Assert.Contains(list!, a =>
            a.OwnerPlayerId == playerId &&
            a.OwnerName == "AnotherPlayer" &&
            a.Kind == "dog" &&
            a.Name == "Doggo"
        );
    }

    [Fact]
    public async Task GetTemplates_ReturnsNonEmptyList()
    {
        // Act
        var resp = await _client.GetAsync("/api/Animals/templates");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var templates = await resp.Content.ReadFromJsonAsync<List<AnimalTemplate>>(JsonOpts());
        Assert.NotNull(templates);
        Assert.NotEmpty(templates!); // will pass now because we seed
    }

    [Fact]
    public async Task ClaimAnimal_WithMissingOwnerPlayerId_Returns400()
    {
        var req = new ClaimAnimalRequest
        {
            OwnerPlayerId = "   ",
            OwnerName = "TestPlayer",
            Kind = "cat",
            Name = "Name"
        };

        var resp = await _client.PostAsJsonAsync("/api/Animals/claim", req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ClaimAnimal_WithUnknownKind_Returns404()
    {
        var req = new ClaimAnimalRequest
        {
            OwnerPlayerId = "p1",
            OwnerName = "TestPlayer",
            Kind = "doesnotexist",
            Name = null
        };

        var resp = await _client.PostAsJsonAsync("/api/Animals/claim", req);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task ClaimAnimal_ClaimingSameKindTwice_Returns409()
    {
        var req = new ClaimAnimalRequest
        {
            OwnerPlayerId = "p-conflict",
            OwnerName = "TestPlayer",
            Kind = "cat",
            Name = null
        };

        var first = await _client.PostAsJsonAsync("/api/Animals/claim", req);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/Animals/claim", req);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ----------------------------
    // Test host + InMemory DB (WITH SEEDING)
    // ----------------------------

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration (SQL Server)
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                );
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Use a unique in-memory database per factory instance
                var dbName = "AnimalsTestsDb_" + Guid.NewGuid();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                // Build provider and seed templates
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Database.EnsureCreated();

                // IMPORTANT: seed templates so Claim + Templates endpoints work
                if (!db.AnimalTemplates.Any())
                {
                    db.AnimalTemplates.AddRange(
                        new AnimalTemplate { Kind = "cat", Attack = 6, Defense = 3, Affection = 4, Level = 1, HpMax = 28 },
                        new AnimalTemplate { Kind = "dog", Attack = 5, Defense = 4, Affection = 3, Level = 1, HpMax = 30 },
                        new AnimalTemplate { Kind = "hamster", Attack = 4, Defense = 5, Affection = 2, Level = 1, HpMax = 32 },
                        new AnimalTemplate { Kind = "fox", Attack = 8, Defense = 5, Affection = 2, Level = 1, HpMax = 34 },
                        new AnimalTemplate { Kind = "owl", Attack = 7, Defense = 6, Affection = 2, Level = 1, HpMax = 36 },
                        new AnimalTemplate { Kind = "boar", Attack = 6, Defense = 8, Affection = 1, Level = 1, HpMax = 40 }
                    );
                    db.SaveChanges();
                }
            });
        }
    }

    private static JsonSerializerOptions JsonOpts() => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class ClaimAnimalRequest
    {
        public string OwnerPlayerId { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string Kind { get; set; } = "";
        public string? Name { get; set; }
    }

    private sealed class PlayerAnimalDto
    {
        public int Id { get; set; }
        public string OwnerPlayerId { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";

        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Affection { get; set; }
        public int Level { get; set; }
        public int HpMax { get; set; }
        public int HpCurrent { get; set; }

        public string CreatedAt { get; set; } = "";
        public int TemplateId { get; set; }
    }
}
