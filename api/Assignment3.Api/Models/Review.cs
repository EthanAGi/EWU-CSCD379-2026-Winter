using System.ComponentModel.DataAnnotations;

namespace Assignment3.Api.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string PlayerName { get; set; } = "";

    [Required]
    [MaxLength(1200)]
    public string Body { get; set; } = "";

    [Range(1, 5)]
    public int Rating { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
