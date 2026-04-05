
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepositiry;

    public BookingService(IBookingRepository bookingRepositiry)
    {
        _bookingRepositiry = bookingRepositiry; 
    }

    public async Task<Guid> CreateBookingAsync(Guid eventId)
    {
        return await _bookingRepositiry.CreateBookingAsync(eventId);
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        return await _bookingRepositiry.GetBookingByIdAsync(bookingId);
    }
}