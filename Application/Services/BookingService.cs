using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingService> _logger;
    private readonly object _bookingLock = new object();
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly int _activeLimit = 9;

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Guid> CreateBookingAsync(Guid userId, Guid eventId, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to create a booking for event with ID: {eventId}");


        var bookingId = Guid.NewGuid();

        var newBooking = Booking.Create(bookingId, userId, eventId);

        await _processingSemaphore.WaitAsync();

        try
        {
            Event? existsEvent = await _eventRepository.GetEventAsync(eventId, token);

            if (existsEvent == null)
                throw new EventNotFoundException(eventId);

            if (existsEvent.StartAt < DateTime.UtcNow)
                throw new BookingForPastEventException(bookingId, existsEvent.Id);

            bool isReserved = existsEvent.TryReserveSeats();
            if (!isReserved)
                throw new NoAvailableSeatsException(eventId);

            var activeBookings = await CountActiveBookingsByUserAsync(userId, token);
            if (activeBookings >= _activeLimit)
                throw new ActiveBookingsLimitExceededException(bookingId, userId);

            await _eventRepository.UpdateEventAsync(existsEvent, token);

            await _bookingRepository.CreateBookingAsync(newBooking, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("Booking process service is stopping due to cancellation request.");
        }
        finally
        {
            _processingSemaphore.Release();
        }

        _logger.LogInformation($"Successfully created booking with ID {bookingId} for event {eventId}.");

        return newBooking.Id;
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

    public async Task BookingProcessAsync(Booking booking, CancellationToken token)
    {
        await _processingSemaphore.WaitAsync();

        Event? eventItem = null;

        try
        {
            eventItem = await _eventRepository.GetEventAsync(booking.EventId, default);
            if (eventItem == null)
                throw new EventNotFoundException(booking.EventId);

            await Confirm(booking, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
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
        Expression<Func<Booking, bool>> predicate = b =>
        (b.Status == Booking.BookingStatus.Pending);

        var result = await _bookingRepository.GetBookingsAsync(predicate, stoppingToken);

        return result;
    }

    public async Task CancelAsync(Guid bookingId, Guid currentUserId, bool isAdmin, CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, stoppingToken);
        if (booking == null)
            throw new BookingNotFoundException(bookingId);

        if (!isAdmin && booking.UserId != currentUserId)
        {
            // Пользователь пытается отменить чужую бронь и не является админом
            throw new  ForbiddenException(currentUserId);
        }

        Event? eventItem = null;
        if (booking.Status == Booking.BookingStatus.Pending || booking.Status == Booking.BookingStatus.Confirmed)
        {
            eventItem = await _eventRepository.GetEventAsync(booking.EventId, stoppingToken);
            // Если событие не найдено — всё равно отменяем бронь, но не трогаем места
        }

        booking.Cancel();

        await _bookingRepository.UpdateBookingAsync(booking, stoppingToken);

        if (eventItem == null)
            return;

        eventItem.ReleaseSeats();

        await _eventRepository.UpdateEventAsync(eventItem, CancellationToken.None);
    }

    private async Task<int> CountActiveBookingsByUserAsync(Guid userId, CancellationToken stoppingToken)
    {
        Expression<Func<Booking, bool>> predicate = b =>
        (b.UserId == userId && b.Status != Booking.BookingStatus.Cancelled && b.Status != Booking.BookingStatus.Rejected);

        var result = await _bookingRepository.GetBookingsAsync(predicate, stoppingToken);

        if (result == null)
            return 0;

        return result.Count;
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
            return;

        eventItem.ReleaseSeats();

        await _eventRepository.UpdateEventAsync(eventItem, CancellationToken.None);
    }
}

