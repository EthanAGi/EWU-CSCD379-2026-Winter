using Assignment3.Api.Models;

namespace Assignment3.Api.Services;

public interface IAnimalService
{
    Task<List<AnimalTemplate>> GetTemplatesAsync();
    Task<List<PlayerAnimal>> GetPlayerAnimalsAsync(string playerId);

    // Claim an animal (implements your business rules)
    Task<PlayerAnimal> ClaimAsync(string ownerPlayerId, string ownerName, string kind, string? name);
}
