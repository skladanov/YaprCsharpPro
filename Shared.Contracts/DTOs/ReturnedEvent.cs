using System.ComponentModel.DataAnnotations;

public class ReturnedEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required]
    public DateTime StartAt { get; set; }
    [Required]
    public DateTime EndAt { get; set; }
    public int? TotalSeats { get; set; }
    public int? AvailableSeats { get; set; }
}