
using System.Linq.Expressions;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingService> _logger;
    private readonly object _bookingLock = new object();

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Guid> CreateBookingAsync(Guid eventId, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to create a booking for event with ID: {eventId}");

        Guid newBookingId = Guid.Empty;
        
        Event? existsEvent = await _eventRepository.GetEventAsync(eventId, token);

        if (existsEvent == null)
            throw new EventNotFoundException(eventId);

        _logger.LogDebug($"Event with ID: {eventId} was found");

        bool isReserved;

        lock (_bookingLock)
        {
            isReserved = existsEvent.TryReserveSeats();
        }

        if(!isReserved)
            throw new NoAvailableSeatsException(eventId);

        var result = await _eventRepository.UpdateEventAsync(existsEvent, token);

        if (!result)
            throw new EventNotFoundException(eventId);

        _logger.LogDebug($"Event with ID: {eventId} was updated");

        newBookingId = await _bookingRepository.CreateBookingAsync(eventId, token);

        _logger.LogInformation($"Successfully created booking with ID {newBookingId} for event {eventId}.");

        return newBookingId;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token)
    {
        _logger.LogInformation("Attempting to retrieve booking with ID: {BookingId}", bookingId);

        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, token);

        if (booking == null)
        {
            _logger.LogWarning("Booking with ID {BookingId} not found.", bookingId);
            throw new BookingNotFoundException(bookingId);
        }

        _logger.LogInformation("Successfully retrieved booking with ID: {BookingId}.", bookingId);

        return booking;
    }
}