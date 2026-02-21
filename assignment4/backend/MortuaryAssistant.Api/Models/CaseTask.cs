using System.ComponentModel.DataAnnotations;

namespace MortuaryAssistant.Api.Models;

public class CaseTask
{
    public int Id { get; set; }

    // Which case this task belongs to
    public int CaseFileId { get; set; }
    public CaseFile CaseFile { get; set; } = null!;

    // Which workflow step this task represents
    public int WorkflowStepTemplateId { get; set; }
    public WorkflowStepTemplate WorkflowStepTemplate { get; set; } = null!;

    // ✅ IMPORTANT: use our enum, not System.Threading.Tasks.TaskStatus
    public CaseTaskStatus Status { get; set; } = CaseTaskStatus.Todo;

    [MaxLength(800)]
    public string? Notes { get; set; }

    // Assignment (optional)
    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}