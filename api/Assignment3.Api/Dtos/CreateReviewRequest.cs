namespace Assignment3.Api.Dtos;

public class CreateReviewRequest
{
    public string PlayerName { get; set; } = "";
    public string Body { get; set; } = "";
    public int Rating { get; set; } = 5;
}
