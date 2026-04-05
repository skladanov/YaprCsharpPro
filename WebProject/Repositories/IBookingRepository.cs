public interface IBookingRepository
{
    Task<Guid> CreateBookingAsync(Guid eventId);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
}