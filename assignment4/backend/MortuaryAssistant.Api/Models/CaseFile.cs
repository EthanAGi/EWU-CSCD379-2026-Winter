namespace MortuaryAssistant.Api.Models;

public class CaseFile
{
    public int Id { get; set; }

    public string CaseNumber { get; set; } = "";

    public CaseStatus Status { get; set; } = CaseStatus.Intake;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ✅ "Public" / basic info you already had
    public string NextOfKinName { get; set; } = "";

    // ✅ New relationships
    public Decedent? Decedent { get; set; }

    public List<CaseTask> Tasks { get; set; } = new();
    public List<CaseNote> Notes { get; set; } = new();

    public List<EquipmentCheckout> EquipmentCheckouts { get; set; } = new();
}