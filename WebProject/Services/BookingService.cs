
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepositiry;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IBookingRepository bookingRepositiry, IEventService eventService, ILogger<BookingService> logger)
    {
        _bookingRepositiry = bookingRepositiry;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<Guid> CreateBookingAsync(Guid id, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to create a booking for event with ID: {id}");

        await _eventService.GetEventAsync(id,  token); //Check event

        Booking booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = id,
            Status = Booking.BookingStatus.Pending,
            CreatedAt = DateTime.Now,
        };

        await _bookingRepositiry.CreateBookingAsync(booking, token);

        _logger.LogInformation($"Successfully created booking with ID {booking.Id} for event {id}.");

        return booking.Id;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token)
    {
        _logger.LogInformation("Attempting to retrieve booking with ID: {BookingId}", bookingId);

        var booking = await _bookingRepositiry.GetBookingByIdAsync(bookingId, token);

        if (booking == null)
        {
            _logger.LogWarning("Booking with ID {BookingId} not found.", bookingId);
            throw new BookingNotFoundException(bookingId);
        }

        _logger.LogInformation("Successfully retrieved booking with ID: {BookingId}.", bookingId);

        return booking;
    }
}