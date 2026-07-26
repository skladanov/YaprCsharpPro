public interface IBookingService
{
    Task<Guid> CreateBookingAsync(Guid userId, Guid eventId, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);

    //Task Confirm(Booking booking, CancellationToken stoppingToken);
    Task CancelAsync(Guid bookingId, Guid userIg, bool isAdmin, CancellationToken stoppingToken);
}