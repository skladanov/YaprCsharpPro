using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

public class BookingRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public BookingRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
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
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10
        );
        context.events.Add(mockEvent);

        var userId = Guid.NewGuid();
        var hasher = new PasswordHasher();
        User mockUser = User.Create(userId, "login", hasher.Hash("secret"), UserRole.Admin);
        context.users.Add(mockUser);

        await context.SaveChangesAsync();

        var bookingId = Guid.NewGuid();
        Booking booking = Booking.Create(bookingId, userId, eventId);
        
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
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10
        );
        context.events.Add(mockEvent);

        var userId = Guid.NewGuid();
        var hasher = new PasswordHasher();
        User mockUser = User.Create(userId, "login", hasher.Hash("secret"), UserRole.Admin);
        context.users.Add(mockUser);

        var bookingId = Guid.NewGuid();
        Booking booking = Booking.Create(bookingId, userId, eventId);
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
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10
        );
        context.events.Add(mockEvent);

        var userId = Guid.NewGuid();
        var hasher = new PasswordHasher();
        User mockUser = User.Create(userId, "login", hasher.Hash("secret"), UserRole.Admin);
        context.users.Add(mockUser);

        Booking booking1 = Booking.Create(Guid.NewGuid(), userId, eventId);
        Booking booking2 = Booking.Create(Guid.NewGuid(), userId, eventId);
        Booking booking3 = Booking.Create(Guid.NewGuid(), userId, eventId);
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
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10
        );
        context.events.Add(mockEvent);

        var userId = Guid.NewGuid();
        var hasher = new PasswordHasher();
        User mockUser = User.Create(userId, "login", hasher.Hash("secret"), UserRole.Admin);
        context.users.Add(mockUser);

        var bookingId = Guid.NewGuid();
        Booking booking = Booking.Create(bookingId, userId, eventId);
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

    // лимиты разных пользователей не влияют друг на друга
    [Fact]
    public async Task CreateBooking_DifferentUsers_LimitsAreIndependent()
    {
        const int activeLimit = 10;

        await _fixture.ResetDatabaseAsync();

        // Arrange
        await using var context = _fixture.CreateContext();

        // Пользователь 1: наберёт лимит
        var user1Id = Guid.NewGuid();
        var hasher = new PasswordHasher();
        User user1 = User.Create(user1Id, "user1", hasher.Hash("secret"), UserRole.User);
        context.users.Add(user1);

        // Пользователь 2: будет создавать бронь после лимита у первого
        var user2Id = Guid.NewGuid();
        User user2 = User.Create(user2Id, "user2", hasher.Hash("secret"), UserRole.User);
        context.users.Add(user2);

        var eventId = Guid.NewGuid();
        Event mockEvent = Event.Create(
            eventId,
            "Event",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            50
        );
        context.events.Add(mockEvent);

        // Набираем лимит для пользователя 1
        for (int i = 0; i < activeLimit; i++)
        {
            var bookingId = Guid.NewGuid();
            Booking booking = Booking.Create(bookingId, user1Id, eventId);
            booking.Confirm(); // или Pending, в зависимости от правила
            context.bookings.Add(booking);
        }

        // У пользователя 2 - только одна бронь
        var user2BookingId = Guid.NewGuid();
        Booking user2Booking = Booking.Create(user2BookingId, user2Id, eventId);
        context.bookings.Add(user2Booking);

        await context.SaveChangesAsync();

        // Act
        await using var updatedContext = _fixture.CreateContext();
        var bookingRepository = new BookingRepository(updatedContext);
        var eventRepository = new EventRepository(updatedContext);
        var mockLogger = new Mock<ILogger<BookingService>>();
        var service = new BookingService(bookingRepository, eventRepository, mockLogger.Object);

        // Попытка создать 11‑ю бронь для пользователя 1 → должна упасть
        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(
            async () =>await service.CreateBookingAsync(user1Id, eventId, default)
        );

        // Попытка создать ещё одну бронь для пользователя 2 → должна пройти
        await service.CreateBookingAsync(user2Id, eventId, default);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var allBookings = await verifyContext.bookings
            .Where(b => b.UserId == user2Id)
            .ToListAsync();

        Assert.NotNull(allBookings);
        Assert.Equal(2, allBookings.Count); // исходная + новая
    }
}