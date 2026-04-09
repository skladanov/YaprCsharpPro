public interface IBookingRepository
{
    Task CreateBookingAsync(Booking booking, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);
    Task<List<Booking>?> GetPendingBookings(CancellationToken token);
}