namespace MortuaryAssistant.Api.Models;

public enum CaseStatus
{
    Intake = 0,
    InPreparation = 1,
    ReadyForViewing = 2,
    ServiceScheduled = 3,
    Completed = 4
}

public class CaseFile
{
    public int Id { get; set; }

    public string CaseNumber { get; set; } = "";

    public CaseStatus Status { get; set; } = CaseStatus.Intake;

    public string DecedentName { get; set; } = "";

    public string NextOfKinName { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
