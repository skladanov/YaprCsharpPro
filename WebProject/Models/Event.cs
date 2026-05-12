using System.ComponentModel.DataAnnotations;

public class Event
{
    public Guid Id { get; init; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int? TotalSeats {  get; private set; }
    public int? AvailableSeats {  get; private set; }
    public ICollection<Booking> Bookings { get; private set; } = null!;
    
    private Event() { }

    public static Event Create(Guid id, string title, DateTime startAt, DateTime endAt, int? totalSeats, string? description = null)
    {
        if (id == Guid.Empty) { throw new ValidationException("ID cannot be null or empty!", nameof(id)); }

        if (String.IsNullOrWhiteSpace(title)) { throw new ValidationException("Title cannot be null or empty!", nameof(title)); }

        if (startAt >= endAt) { throw new ValidationException("Start date must be before end date", nameof(startAt)); }

        if (totalSeats == null) { throw new ValidationException("TotalSeats cannot be null or empty!", nameof(totalSeats)); }

        if (totalSeats < 1) { throw new ValidationException("TotalSeats must be more than zero!", nameof(totalSeats)); }

        return new Event()
        {
            Id = id,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
     }

    public bool TryReserveSeats(int count = 1)
    {
        if (count < 1) 
            throw new ArgumentException("Count of reserve seats must be more then zero!", nameof(count));
        if (count > AvailableSeats) return false; 

        AvailableSeats -= count;

        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        if (count < 1)
            throw new ArgumentException("Count of release seats must be more then zero!", nameof(count));

        if (TotalSeats - (AvailableSeats + count) < 0)
            throw new ArgumentException("Count of release seats must be more then zero!", nameof(count));

        AvailableSeats += count;
    }
}