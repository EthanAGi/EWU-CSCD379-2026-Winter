namespace Assignment3.Api.Dtos;

public class ReviewDto
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = "";
    public string Body { get; set; } = "";
    public int Rating { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
