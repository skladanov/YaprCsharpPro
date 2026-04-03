public class LocalBookingRepository : IBookingRepository
{ 
    List<Booking> _bookings = new();
    private int _nextId = 1;

    public async Task<int> CreateBookingAsync(int eventId)
    {
        Booking booking = new Booking
        {
            Id = _nextId++,
            EventId = eventId,
            Status = Booking.BookingStatus.Pending,
            CreatedAt = DateTime.Now,
        };

        _bookings.Add(booking);
        return await Task.FromResult(booking.Id);
    }

    public async Task<Booking?> GetBookingByIdAsync(int bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        return await Task.FromResult(booking);
    }
}