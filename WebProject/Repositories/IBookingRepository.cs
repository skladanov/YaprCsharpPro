using System.Linq.Expressions;

public interface IBookingRepository
{
    Task CreateBookingAsync(Booking booking, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);
    Task<List<Booking>?> GetBookingsAsync(Expression<Func<Booking, bool>> predicate, CancellationToken token);
}