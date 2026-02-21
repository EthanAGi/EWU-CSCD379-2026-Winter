using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MortuaryAssistant.Api.Constants;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/mortician")]
[Authorize(Roles = Roles.Mortician)]
public class MorticianController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "Mortician access confirmed." });
}