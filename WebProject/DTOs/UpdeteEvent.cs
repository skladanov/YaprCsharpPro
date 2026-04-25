using System.ComponentModel.DataAnnotations;

public class UpdateEvent
{
    [Required]
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    [Required]
    public DateTime StartAt { get; set; }
    [Required]
    public DateTime EndAt { get; set; }
}