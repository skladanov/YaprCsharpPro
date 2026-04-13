public interface IBookingService
{
    Task<Guid> CreateBookingAsync(Guid eventId, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);
    Task<List<Booking>?> GetBookingsByStatusAsync(Booking.BookingStatus status, CancellationToken token);
    Task BookingProcessAsync(Booking booking, CancellationToken token);
}