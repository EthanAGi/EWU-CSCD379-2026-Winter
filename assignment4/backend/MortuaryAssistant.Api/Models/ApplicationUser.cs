using Microsoft.AspNetCore.Identity;

namespace MortuaryAssistant.Api.Models;

public class ApplicationUser : IdentityUser
{
    // Optional for display
    public string? DisplayName { get; set; }
}