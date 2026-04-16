using System.Linq.Expressions;

public class LocalBookingRepository : IBookingRepository
{ 
    List<Booking> _bookings = new();

    public async Task CreateBookingAsync(Booking booking, CancellationToken token)
    {
        _bookings.Add(booking);
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
}