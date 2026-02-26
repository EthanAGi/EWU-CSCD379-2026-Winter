using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Services;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/mortician")]
[Authorize(Roles = Roles.Mortician)]
public class MorticianController : ControllerBase
{
    private readonly ICaseService _cases;

    public MorticianController(ICaseService cases)
    {
        _cases = cases;
    }

    // This endpoint is primarily an auth/role gate ("can a Mortician reach this?")
    // But now it also verifies the service is wired through DI.
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        // "Use" the service without assuming any method exists on it.
        // This avoids compile breaks from guessing method names.
        var serviceWired = _cases is not null;

        return Ok(new
        {
            message = "Mortician access confirmed.",
            serviceWired
        });
    }
}