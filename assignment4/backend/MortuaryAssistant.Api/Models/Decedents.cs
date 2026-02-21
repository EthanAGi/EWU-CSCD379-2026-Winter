using System.ComponentModel.DataAnnotations;

namespace MortuaryAssistant.Api.Models;

public class Decedent
{
    public int Id { get; set; }

    // ✅ 1-to-1 with CaseFile
    public int CaseFileId { get; set; }
    public CaseFile CaseFile { get; set; } = null!;

    [MaxLength(80)]
    public string FirstName { get; set; } = "";

    [MaxLength(80)]
    public string LastName { get; set; } = "";

    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }

    [MaxLength(120)]
    public string? PlaceOfDeath { get; set; }

    // Useful tracking fields
    [MaxLength(80)]
    public string? TagNumber { get; set; }

    [MaxLength(80)]
    public string? StorageLocation { get; set; } // “Cooler A - Shelf 2”
}