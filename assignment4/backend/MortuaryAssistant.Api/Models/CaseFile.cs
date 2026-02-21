namespace MortuaryAssistant.Api.Models;

public class CaseFile
{
    public int Id { get; set; }

    public string CaseNumber { get; set; } = "";

    public CaseStatus Status { get; set; } = CaseStatus.Intake;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string NextOfKinName { get; set; } = "";

    // ✅ Assigned Mortician
    public string? AssignedMorticianUserId { get; set; }
    public ApplicationUser? AssignedMortician { get; set; }

    public Decedent? Decedent { get; set; }

    public List<CaseTask> Tasks { get; set; } = new();
    public List<CaseNote> Notes { get; set; } = new();

    public List<EquipmentCheckout> EquipmentCheckouts { get; set; } = new();
}