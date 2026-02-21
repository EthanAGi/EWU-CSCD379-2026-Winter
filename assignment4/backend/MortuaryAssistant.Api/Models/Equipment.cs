using System.ComponentModel.DataAnnotations;

namespace MortuaryAssistant.Api.Models;

public class Equipment
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(120)]
    public string? SerialNumber { get; set; }

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
}