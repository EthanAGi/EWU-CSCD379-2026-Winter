using System.ComponentModel.DataAnnotations;

namespace MortuaryAssistant.Api.Models;

public class CaseNote
{
    public int Id { get; set; }

    public int CaseFileId { get; set; }
    public CaseFile CaseFile { get; set; } = null!;

    [MaxLength(1200)]
    public string Text { get; set; } = "";

    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}