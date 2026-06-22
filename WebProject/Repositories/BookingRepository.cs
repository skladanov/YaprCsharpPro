using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebProject.DataAccess;

public class BookingRepository : IBookingRepository
{ 
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateBookingAsync(Booking newBooking, CancellationToken token)
    {
        _context.bookings.Add(newBooking);
        await _context.SaveChangesAsync(token);
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token)
    {
        return await _context.bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<List<Booking>?> GetBookingsAsync(Expression<Func<Booking, bool>> predicate, CancellationToken token)
    {
        return await _context.bookings.AsQueryable().Where(predicate).ToListAsync(token);
    }

    public async Task UpdateBookingAsync(Booking updatedBooking, CancellationToken token)
    {
        _context.bookings.Update(updatedBooking);
        await _context.SaveChangesAsync(token);
    }
}