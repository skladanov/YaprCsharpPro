public interface IBookingService
{
    Task<int> CreateBookingAsync(int eventId);
    Task<Booking?> GetBookingByIdAsync(int bookingId);
}