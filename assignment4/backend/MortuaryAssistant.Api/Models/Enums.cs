namespace MortuaryAssistant.Api.Models;

public enum CaseStatus
{
    Intake = 0,
    InPreparation = 1,
    ReadyForViewing = 2,
    ServiceScheduled = 3,
    Completed = 4
}

// ✅ Renamed to avoid clash with System.Threading.Tasks.TaskStatus
public enum CaseTaskStatus
{
    Todo = 0,
    InProgress = 1,
    Blocked = 2,
    Done = 3
}

public enum EquipmentStatus
{
    Available = 0,
    InUse = 1,
    Maintenance = 2,
    Retired = 3
}