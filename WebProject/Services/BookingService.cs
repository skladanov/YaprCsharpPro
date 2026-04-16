
using System.Linq.Expressions;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IBookingRepository bookingRepository, IEventService eventService, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
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
            CreatedAt = DateTime.UtcNow
        };

        await _bookingRepository.CreateBookingAsync(booking, token);

        _logger.LogInformation($"Successfully created booking with ID {booking.Id} for event {id}.");

        return booking.Id;
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

    public async Task<List<Booking>?> GetBookingsByStatusAsync(Booking.BookingStatus status, CancellationToken stoppingToken)
    {
        Expression<Func<Booking, bool>> predicate = e =>
            (e.Status == status);

        var result = await _bookingRepository.GetBookingsAsync(predicate, stoppingToken);

        return result;
    }

    public async Task BookingProcessAsync(Booking booking, CancellationToken stoppingToken)
    {
        try
        {
            booking.Status = Booking.BookingStatus.Confirmed;
            booking.ProcessedAt = DateTime.Now;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            booking.Status = Booking.BookingStatus.Rejected;
            _logger.LogInformation("Booking process service is stopping due to cancellation request.");
            return;
        }
        catch (Exception ex)
        {
            booking.Status = Booking.BookingStatus.Rejected;
            _logger.LogError(ex, $"An error occurred while processing bookings: {ex.Message}");
            return;
        }

        _logger.LogInformation($"Successfully processed bookings with ID: {booking.Id}.");
    }
}