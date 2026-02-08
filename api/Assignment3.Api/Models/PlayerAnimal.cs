namespace Assignment3.Api.Models;

public class PlayerAnimal
{
    public int Id { get; set; }                    // PK

    // who owns it (match your frontend Player.id / Player.name)
    public string OwnerPlayerId { get; set; } = "";
    public string OwnerName { get; set; } = "";

    // display name like: "Ethan's Fox"
    public string Name { get; set; } = "";

    public string Kind { get; set; } = "";         // keep for convenience ("fox")

    // stats copied from template at time of purchase
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Affection { get; set; }
    public int Level { get; set; }
    public int HpMax { get; set; }
    public int HpCurrent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK to template (optional but recommended)
    public int TemplateId { get; set; }
    public AnimalTemplate? Template { get; set; }
}
