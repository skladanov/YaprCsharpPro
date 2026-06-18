using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using WebProject.DataAccess;
using Xunit;

public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventapi")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
    
    [Fact]
    public async Task CreateBooking_ReturnsBookingWithPendingStatus()
    {
        await ResetDatabaseAsync();
        
        // Arrange
        await using var context = CreateContext();
        
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
        Booking booking = Booking.Create(bookingId, eventId);
        
        await context.SaveChangesAsync();
        
        // Act
        var repository = new BookingRepository(CreateContext());
        await repository.CreateBookingAsync(booking, default);
        
        // Assert
        await using var verifyContext = CreateContext();
        var result = verifyContext.bookings.FirstOrDefault(e => e.Id == booking.Id);
        Assert.NotNull(result);
        Assert.Equal(Booking.BookingStatus.Pending, result.Status);
        Assert.Equal(eventId, result.EventId);
    }

    [Fact]
    public async Task GetBookingById_ForExistingBooking_ReturnsBookingSuccesed()
    {
        await ResetDatabaseAsync();
        
        // Arrange
        await using var context = CreateContext();
        
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
        Booking booking = Booking.Create(bookingId, eventId);
        context.bookings.Add(booking);
        
        await context.SaveChangesAsync();
        
        // Act
        var repository = new BookingRepository(CreateContext());
        Booking result = await repository.GetBookingByIdAsync(booking.Id,  default);
        
        // Assert
        await using var verifyContext = CreateContext();
        Assert.NotNull(result);
        Assert.Equal(Booking.BookingStatus.Pending, result.Status);
        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetpendingBookings_ForExistingBookings_ReturnsAllBookings()
    {
        await ResetDatabaseAsync();
        
        // Arrange
        await using var context = CreateContext();
        
        var eventId = Guid.NewGuid();
        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );
        context.events.Add(mockEvent);
        
        Booking booking1 = Booking.Create(Guid.NewGuid(), eventId);
        Booking booking2 = Booking.Create(Guid.NewGuid(), eventId);
        Booking booking3 = Booking.Create(Guid.NewGuid(), eventId);
        context.bookings.Add(booking1);
        context.bookings.Add(booking2);
        context.bookings.Add(booking3);
        
        await context.SaveChangesAsync();
        
        // Act
        var repository = new BookingRepository(CreateContext());
        Expression<Func<Booking, bool>> predicate = e =>
            (e.Status == Booking.BookingStatus.Pending);
        List<Booking> result = await repository.GetBookingsAsync(predicate,  default);
        
        // Assert
        await using var verifyContext = CreateContext();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
    
    //UpdateBooking
    [Fact]
    public async Task UpdateBookingStatus_ForExistingBooking_ReturnsUpdatedBooking()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        
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
        Booking booking = Booking.Create(bookingId, eventId);
        context.bookings.Add(booking);
        
        await context.SaveChangesAsync();

        // Act
        var repository = new BookingRepository(CreateContext());
        booking.Confirm();
        await repository.UpdateBookingAsync(booking, default);

        // Assert
        await using var verifyContext = CreateContext();
        Booking result = await verifyContext.bookings.FirstOrDefaultAsync(e => e.Id == booking.Id);
        Assert.NotNull(result);
        Assert.Equal(Booking.BookingStatus.Confirmed, result.Status);
    }
}