using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Xunit;

public class BookingRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly Guid _userId;

    public BookingRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _userId = Guid.NewGuid();
    }

    [Fact]
    public async Task CreateBooking_ReturnsBookingWithPendingStatus()
    {
        await _fixture.ResetDatabaseAsync();
        
        // Arrange
        await using var context = _fixture.CreateContext();
        
        var eventId = Guid.NewGuid();
        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );
        context.events.Add(mockEvent);
        
        var bookingId = Guid.NewGuid();
        Booking booking = Booking.Create(_userId, bookingId, eventId);
        
        await context.SaveChangesAsync();
        
        // Act
        var repository = new BookingRepository(_fixture.CreateContext());
        await repository.CreateBookingAsync(booking, default);
        
        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var result = verifyContext.bookings.FirstOrDefault(e => e.Id == booking.Id);
        Assert.NotNull(result);
        Assert.Equal(Booking.BookingStatus.Pending, result.Status);
        Assert.Equal(eventId, result.EventId);
    }

    [Fact]
    public async Task GetBookingById_ForExistingBooking_ReturnsBookingSuccesed()
    {
        await _fixture.ResetDatabaseAsync();
        
        // Arrange
        await using var context = _fixture.CreateContext();
        
        var eventId = Guid.NewGuid();
        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );
        context.events.Add(mockEvent);
        
        var bookingId = Guid.NewGuid();
        Booking booking = Booking.Create(_userId, bookingId, eventId);
        context.bookings.Add(booking);
        
        await context.SaveChangesAsync();
        
        // Act
        var repository = new BookingRepository(_fixture.CreateContext());
        Booking result = await repository.GetBookingByIdAsync(booking.Id,  default);
        
        // Assert
        await using var verifyContext = _fixture.CreateContext();
        Assert.NotNull(result);
        Assert.Equal(Booking.BookingStatus.Pending, result.Status);
        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetpendingBookings_ForExistingBookings_ReturnsAllBookings()
    {
        await _fixture.ResetDatabaseAsync();
        
        // Arrange
        await using var context = _fixture.CreateContext();
        
        var eventId = Guid.NewGuid();
        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );
        context.events.Add(mockEvent);
        
        Booking booking1 = Booking.Create(_userId, Guid.NewGuid(), eventId);
        Booking booking2 = Booking.Create(_userId, Guid.NewGuid(), eventId);
        Booking booking3 = Booking.Create(_userId, Guid.NewGuid(), eventId);
        context.bookings.Add(booking1);
        context.bookings.Add(booking2);
        context.bookings.Add(booking3);
        
        await context.SaveChangesAsync();
        
        // Act
        var repository = new BookingRepository(_fixture.CreateContext());
        Expression<Func<Booking, bool>> predicate = e =>
            (e.Status == Booking.BookingStatus.Pending);
        List<Booking> result = await repository.GetBookingsAsync(predicate,  default);
        
        // Assert
        await using var verifyContext = _fixture.CreateContext();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
    
    //UpdateBooking
    [Fact]
    public async Task UpdateBookingStatus_ForExistingBooking_ReturnsUpdatedBooking()
    {
        await _fixture.ResetDatabaseAsync();

        // Arrange
        await using var context = _fixture.CreateContext();
        
        var eventId = Guid.NewGuid();
        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );
        context.events.Add(mockEvent);
        
        var bookingId = Guid.NewGuid();
        Booking booking = Booking.Create(_userId, bookingId, eventId);
        context.bookings.Add(booking);
        
        await context.SaveChangesAsync();

        // Act
        var repository = new BookingRepository(_fixture.CreateContext());
        booking.Confirm();
        await repository.UpdateBookingAsync(booking, default);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        Booking result = await verifyContext.bookings.FirstOrDefaultAsync(e => e.Id == booking.Id);
        Assert.NotNull(result);
        Assert.Equal(Booking.BookingStatus.Confirmed, result.Status);
    }
}