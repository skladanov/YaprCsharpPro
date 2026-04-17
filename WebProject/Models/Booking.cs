using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Booking
{
    private Booking() { }

    public static Booking Create(Guid id, Guid eventId)
    {
        if (id == Guid.Empty) 
            throw new ArgumentNullException("BookingID cannot be null or empty", nameof(id));

        if (eventId == Guid.Empty)
            throw new ArgumentNullException("EventID cannot be null or empty", nameof(id));

        return new Booking()
        {
            Id = id,
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.Now
        };
    }

    [Required]
    public Guid Id { get; init; }
    [Required]
    public Guid EventId {  get; init; }
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BookingStatus Status {  get; private set; }
    [Required]
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.Now;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = null;
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Rejected
    }
}