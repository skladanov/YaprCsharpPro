public interface IBookingRepository
{
    Task<int> CreateBookingAsync(int eventId);
    Task<Booking?> GetBookingByIdAsync(int bookingId);
}