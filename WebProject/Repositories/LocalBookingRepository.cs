using System.Linq.Expressions;

public class LocalBookingRepository : IBookingRepository
{ 
    List<Booking> _bookings = new();

    public async Task<Guid> CreateBookingAsync(Guid eventId, CancellationToken token)
    {
        var bookingId = Guid.NewGuid();

        var newBooking = Booking.Create(bookingId, eventId);

        _bookings.Add(newBooking);

        return bookingId;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        return booking;
    }

    public async Task<List<Booking>?> GetBookingsAsync(Expression<Func<Booking, bool>> predicate, CancellationToken token)
    {
        return _bookings.AsQueryable().Where(predicate).ToList();
    }

    public async Task<bool> UpdateBookingAsync(Booking updatedBooking, CancellationToken token)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == updatedBooking.Id);

        if (booking == null)
            return false;

        booking = updatedBooking;
        return true;
    }
}