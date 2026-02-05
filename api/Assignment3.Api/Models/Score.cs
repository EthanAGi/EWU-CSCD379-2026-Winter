namespace Assignment3.Api.Models;

public class Score
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
