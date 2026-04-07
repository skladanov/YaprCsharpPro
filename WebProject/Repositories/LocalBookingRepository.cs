public class LocalBookingRepository : IBookingRepository
{ 
    List<Booking> _bookings = new();

    public async Task<Guid> CreateBookingAsync(Guid eventId)
    {
        Booking booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = Booking.BookingStatus.Pending,
            CreatedAt = DateTime.Now,
        };

        _bookings.Add(booking);
        return await Task.FromResult(booking.Id);
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        return await Task.FromResult(booking);
    }

    public async Task<List<Booking>?> GetPendingBookings()
    {
        return _bookings.AsQueryable().Where(b => b.Status == Booking.BookingStatus.Pending).ToList();
    }
}