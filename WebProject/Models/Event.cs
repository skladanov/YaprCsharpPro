using System.ComponentModel.DataAnnotations;

public class Event
{
    private Event() { }

    public static Event Create(Guid id, string title, DateTime startAt, DateTime endAt, string? description = null)
    {
        return new Event()
        {
            Id = id,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt
        };
     }

    [Required]
    public Guid Id { get; init; }
    [Required]
    public required string Title { get; set; }
    public string? Description { get; set; }
    [Required]
    public DateTime StartAt { get; set; }
    [Required]
    public DateTime EndAt { get; set; }
}