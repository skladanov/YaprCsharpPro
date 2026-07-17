using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingService> _logger;
    private readonly IBookingProducer _producer;
    private readonly int _activeLimit = 10;

    public BookingService(IBookingRepository bookingRepository, IBookingProducer producer, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _producer = producer;
        _logger = logger;
    }

    public async Task<Guid> CreateBookingAsync(Guid userId, Guid eventId, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to create a booking for event with ID: {eventId}");

        var bookingId = Guid.NewGuid();

        var newBooking = Booking.Create(bookingId, userId, eventId);

        var activeBookings = await CountActiveBookingsByUserAsync(userId, token);
        if (activeBookings >= _activeLimit)
            throw new ActiveBookingsLimitExceededException(bookingId, userId);

        await _bookingRepository.CreateBookingAsync(newBooking, token);

        await _producer.PublishAsync(
            new BookingCreatedEvent(eventId, SeatsCount: 1),
            token
        );

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

    public async Task CancelAsync(Guid bookingId, Guid currentUserId, bool isAdmin, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to cancel a booking with ID: {bookingId}");

        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, token);
        if (booking == null)
            throw new BookingNotFoundException(bookingId);

        if (!isAdmin && booking.UserId != currentUserId)
        {
            throw new  ForbiddenException(currentUserId);
        }

        booking.Cancel();

        await _bookingRepository.UpdateBookingAsync(booking, token);

        await _producer.PublishAsync(
            new BookingCanceledEvent(booking.EventId, 1),
            token
        );

        _logger.LogInformation($"Successfully canceled booking with ID {bookingId}.");
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
}

