public interface IBookingService
{
    Task<Guid> CreateBookingAsync(Guid userId, Guid eventId, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);
    Task<List<Booking>?> GetPendingsAsync(CancellationToken stoppingToken);
    Task BookingProcessAsync(Booking booking, CancellationToken stoppingToken);
    Task Confirm(Booking booking, CancellationToken stoppingToken);
    Task Reject(Booking booking, Event eventItem);
    Task CancelAsync(Guid userId, Guid bookingId, CancellationToken stoppingToken);
}