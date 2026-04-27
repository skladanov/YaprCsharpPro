using System.Linq.Expressions;

public interface IBookingRepository
{
    Task<Guid> CreateBookingAsync(Guid eventId, CancellationToken token);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token);
    Task<List<Booking>?> GetBookingsAsync(Expression<Func<Booking, bool>> predicate, CancellationToken token);
    Task<bool> UpdateBookingAsync(Booking booking, CancellationToken token);
}