using System.ComponentModel.DataAnnotations;

namespace MortuaryAssistant.Api.Models;

public class WorkflowStepTemplate
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(400)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}