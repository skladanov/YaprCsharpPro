public class Booking
{
    public Guid Id { get; init; }
    public BookingStatus Status {  get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public Guid EventId {  get; init; }
    public Guid UserId { get; init; }

    private Booking() { }

    public static Booking Create(Guid id, Guid userId, Guid eventId)
    {
        if (id == Guid.Empty) 
            throw new ArgumentNullException("BookingID cannot be null or empty", nameof(id));

        if (eventId == Guid.Empty)
            throw new ArgumentNullException("EventID cannot be null or empty", nameof(id));

        if (userId == Guid.Empty)
            throw new ArgumentNullException("UserID cannot be null or empty", nameof(id));

        return new Booking()
        {
            Id = id,
            UserId = userId,
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = null;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new BookingAlreadyCancelledException(Id);

        Status = BookingStatus.Cancelled;
        ProcessedAt = null;
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Rejected,
        Cancelled
    }
}