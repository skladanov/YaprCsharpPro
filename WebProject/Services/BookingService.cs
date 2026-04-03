
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepositiry;

    public BookingService(IBookingRepository bookingRepositiry)
    {
        _bookingRepositiry = bookingRepositiry; 
    }

    public async Task<int> CreateBookingAsync(int eventId)
    {
        return await _bookingRepositiry.CreateBookingAsync(eventId);
    }

    public async Task<Booking?> GetBookingByIdAsync(int bookingId)
    {
        return await _bookingRepositiry.GetBookingByIdAsync(bookingId);
    }
}