using System.ComponentModel.DataAnnotations;

public class Booking
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int EventId {  get; set; }
    [Required]
    public BookingStatus Status {  get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Rejected
    }
}