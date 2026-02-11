using Assignment3.Api.Models;
using Assignment3.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Assignment3.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly IAnimalService _animals;

    public AnimalsController(IAnimalService animals)
    {
        _animals = animals;
    }

    // GET /api/animals/templates
    [HttpGet("templates")]
    public async Task<ActionResult<List<AnimalTemplate>>> GetTemplates()
    {
        var templates = await _animals.GetTemplatesAsync();
        return Ok(templates);
    }

    // GET /api/animals/player/{playerId}
    [HttpGet("player/{playerId}")]
    public async Task<ActionResult<List<PlayerAnimal>>> GetPlayerAnimals(string playerId)
    {
        var animals = await _animals.GetPlayerAnimalsAsync(playerId);
        return Ok(animals);
    }

    // POST /api/animals/claim
    // { ownerPlayerId, ownerName, kind, name }
    public record ClaimAnimalRequest(string OwnerPlayerId, string OwnerName, string Kind, string? Name);

    [HttpPost("claim")]
    public async Task<ActionResult<PlayerAnimal>> Claim([FromBody] ClaimAnimalRequest req)
    {
        try
        {
            var created = await _animals.ClaimAsync(req.OwnerPlayerId, req.OwnerName, req.Kind, req.Name);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // using InvalidOperationException here to represent "conflict" rule violations
            return Conflict(ex.Message);
        }
    }
}
