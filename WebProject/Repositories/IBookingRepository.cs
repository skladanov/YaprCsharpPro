public interface IBookingRepository
{
    Task<Guid> CreateBookingAsync(Guid eventId, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);
    Task<List<Booking>?> GetPendingBookings(CancellationToken token);
}