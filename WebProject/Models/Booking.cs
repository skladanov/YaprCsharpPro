using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Booking
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public Guid EventId {  get; set; }
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
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