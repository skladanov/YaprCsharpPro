
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepositiry;
    private readonly IEventService _eventService;

    public BookingService(IBookingRepository bookingRepositiry, IEventService eventService)
    {
        _bookingRepositiry = bookingRepositiry;
        _eventService = eventService;
    }

    public async Task<Guid> CreateBookingAsync(Guid id, CancellationToken token)
    {
        var eventItem = await _eventService.GetEventAsync(id,  token);
        if (eventItem == null || eventItem.Id != id)
            throw new EventNotFoundException(id);

        var bookingId = await _bookingRepositiry.CreateBookingAsync(id, token);
        return bookingId;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token)
    {
        var booking = await _bookingRepositiry.GetBookingByIdAsync(bookingId, token);
        if (booking == null)
            throw new BookingNotFoundException(bookingId);
        return booking;
    }
}