
using System.Linq.Expressions;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingService> _logger;
    private readonly object _bookingLock = new object();
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Guid> CreateBookingAsync(Guid eventId, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to create a booking for event with ID: {eventId}");

        var newBookingId = Guid.Empty;

        var bookingId = Guid.NewGuid();

        var newBooking = Booking.Create(bookingId, eventId);

        lock (_bookingLock)
        {
            Event? existsEvent = _eventRepository.GetEventAsync(eventId, token).Result;

            if (existsEvent == null)
                throw new EventNotFoundException(eventId);

            bool isReserved = existsEvent.TryReserveSeats();

            if (!isReserved)
                throw new NoAvailableSeatsException(eventId);

            bool result = _eventRepository.UpdateEventAsync(existsEvent, token).Result;

            if (!result)
                throw new EventNotFoundException(eventId);

            newBookingId = _bookingRepository.CreateBookingAsync(newBooking, token).Result;
        }

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

    public async Task BookingProcessAsync(Booking booking, CancellationToken stoppingToken)
    {
        await _processingSemaphore.WaitAsync();

        Event? eventItem = null;

        try
        {
            eventItem = await _eventRepository.GetEventAsync(booking.EventId, default);

            await Confirm(booking, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await Reject(booking, eventItem);

            _logger.LogInformation("Booking process service is stopping due to cancellation request.");
        }
        catch (Exception ex)
        {
            await Reject(booking, eventItem);
            
            _logger.LogError(ex, $"An error occurred while processing bookings: {ex.Message}");
        }
        finally
        {
            _processingSemaphore.Release();
        }

        _logger.LogInformation($"Successfully processed bookings with ID: {booking.Id}.");
    }

    public async Task<List<Booking>?> GetPendingsAsync(CancellationToken stoppingToken)
    {
        Expression<Func<Booking, bool>> predicate = e =>
        (e.Status == Booking.BookingStatus.Pending);

        var result = await _bookingRepository.GetBookingsAsync(predicate, stoppingToken);

        return result;
    }

    public async Task Confirm(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        booking.Confirm();

        await _bookingRepository.UpdateBookingAsync(booking, stoppingToken);
    }

    public async Task Reject(Booking booking, Event? eventItem)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        booking.Reject();

        await _bookingRepository.UpdateBookingAsync(booking, CancellationToken.None);

        if (eventItem == null)
        {
            _logger.LogWarning("Probably booking was rejected, because event not found");

            return;
        }

        eventItem.ReleaseSeats();

        bool isEventUpdated = await _eventRepository.UpdateEventAsync(eventItem, CancellationToken.None);

        if (!isEventUpdated)
        {
            _logger.LogError($"An error occurred while release seats");
        }
    }
}