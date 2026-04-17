using System.ComponentModel.DataAnnotations;

public class Event
{
    private Event() { }

    public static Event Create(Guid id, string title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
    {
        if (id == Guid.Empty) { throw new ArgumentNullException("ID cannot be null or empty!", nameof(id)); }

        if (String.IsNullOrWhiteSpace(title)) { throw new ArgumentNullException("Title cannot be null or empty!", nameof(title)); }

        if (startAt >= endAt) { throw new ArgumentException("Start date must be before end date", nameof(startAt)); }

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

    [Required]
    public Guid Id { get; init; }
    [Required]
    public required string Title { get; set; }
    public string? Description { get; set; }
    [Required]
    public DateTime StartAt { get; set; }
    [Required]
    public DateTime EndAt { get; set; }
    [Required]
    public int TotalSeats {  get; private set; }
    public int AvailableSeats {  get; private set; }

    public bool TryReserveSeats(int count = 1)
    {
        if (count < 1) 
            throw new ArgumentException("Count of reserve seats must be more then zero!", nameof(count));
        if (count > AvailableSeats) return false; 

        AvailableSeats -= count;

        return true;
    }
}