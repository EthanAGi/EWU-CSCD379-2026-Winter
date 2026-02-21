using System.ComponentModel.DataAnnotations;

namespace MortuaryAssistant.Api.Models;

public class EquipmentCheckout
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    // Optional: tie to a case
    public int? CaseFileId { get; set; }
    public CaseFile? CaseFile { get; set; }

    [Required]
    public string CheckedOutByUserId { get; set; } = "";

    public DateTime CheckedOutAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }

    [MaxLength(400)]
    public string? Notes { get; set; }
}