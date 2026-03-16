using System.ComponentModel.DataAnnotations;

public class Event
{
    [Required]
    public int Id { get; set; }
    [Required]
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    [Required]
    public DateTime StartAt { get; set; }
    [Required]
    public DateTime EndAt { get; set; }
}