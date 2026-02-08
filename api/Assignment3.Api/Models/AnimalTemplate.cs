namespace Assignment3.Api.Models;

public class AnimalTemplate
{
    public int Id { get; set; }                 // PK
    public string Kind { get; set; } = "";      // "dog" | "cat" | ...
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Affection { get; set; }
    public int Level { get; set; }
    public int HpMax { get; set; }
}
