public interface IBookingService
{
    Task<Guid> CreateBookingAsync(Guid eventId, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);
}