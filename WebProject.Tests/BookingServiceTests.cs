using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using static Booking;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<ILogger<BookingService>> _mockLogger;
    private readonly IBookingService _service;

    public BookingServiceTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockEventService = new Mock<IEventService>();
        _mockLogger = new Mock<ILogger<BookingService>>();
        _service = new BookingService(_mockRepository.Object, _mockEventService.Object, _mockLogger.Object);
    }
    //Успешные сценарии:

    //создание брони для существующего события — возвращается BookingInfo со статусом Pending;
    [Fact]
    public async Task CreateBooking_ForExistingEvent_ReturnsPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.Now,
            DateTime.Now.AddDays(1),
            10
        );


        Guid resultBookingId = Guid.NewGuid();

        _mockEventService.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);

        _mockRepository.Setup(r => r.CreateBookingAsync(eventId, default)).ReturnsAsync(resultBookingId);

        // Act
        var result = await _service.CreateBookingAsync(eventId, default);

        // Assert
        _mockEventService.Verify(s => s.GetEventAsync(eventId, default),Times.Once());
        Assert.Equal(resultBookingId, result);
    }

    //создание нескольких броней для одного события — все создаются с уникальными Id;
    [Fact]
    public async Task CreateSomeBookings_ForExistingEvent_ReturnsBookingsWithOtherIDs()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.Now,
            DateTime.Now.AddDays(1),
            10
        );

        _mockEventService.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);

        _mockRepository.Setup(r => r.CreateBookingAsync(eventId, default)).ReturnsAsync(() => Guid.NewGuid());

        // Act
        var result1 = await _service.CreateBookingAsync(eventId, default);
        var result2 = await _service.CreateBookingAsync(eventId, default);

        // Assert
        _mockEventService.Verify(s => s.GetEventAsync(eventId, default), Times.AtMost(3));
        Assert.NotEqual(result1, result2);
    }

    //получение брони по Id — возвращается корректная информация;
    //получение брони отражает изменение статуса(после Confirm/Reject).
    [Fact]
    public async Task GetBookingById_ForExistingBooking_ReturnsBookings()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var initialBooking = Booking.Create(bookingId, eventId);

        _mockRepository
            .SetupSequence(r => r.GetBookingByIdAsync(bookingId, default))
            .ReturnsAsync(initialBooking)
            .ReturnsAsync(initialBooking);

        // Act 1
        var firstResult = await _service.GetBookingByIdAsync(bookingId, default);

        // Assert 1
        Assert.NotNull(firstResult);
        Assert.Equal(BookingStatus.Pending, firstResult.Status);
        Assert.Null(firstResult.ProcessedAt);

        // Act 2
        initialBooking.Confirm();
        var secondResult = await _service.GetBookingByIdAsync(bookingId, default);

        // Assert 2
        Assert.NotNull(secondResult);
        Assert.Equal(BookingStatus.Confirmed, secondResult.Status);
        Assert.NotNull(secondResult.ProcessedAt);
        Assert.True((secondResult.ProcessedAt.Value - DateTime.Now).Duration() <= TimeSpan.FromSeconds(3));
    }


    //Неуспешные сценарии:
    //создание брони для несуществующего события;
    //создание брони для удалённого события;
    [Fact]
    public async Task CreateBooking_ForNonExistentEvent_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _mockEventService.Setup(s => s.GetEventAsync(eventId, default)).ThrowsAsync(new EventNotFoundException(eventId));

        _mockRepository.Setup(r => r.CreateBookingAsync(eventId, default));


        // Assert
        var ex = await Assert.ThrowsAsync<EventNotFoundException>(
            async () => await _service.CreateBookingAsync(eventId, default));

        _mockEventService.Verify(s => s.GetEventAsync(eventId, default), Times.Once());
        _mockRepository.Verify(r => r.CreateBookingAsync(eventId, default), Times.Never());
    }


    //получение брони по несуществующему Id
    [Fact]
    public async Task GetBooking_ForNonExistentBooking_Returns404NotFound()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetBookingByIdAsync(bookingId, default))
            .ReturnsAsync((Booking?)null);

        // Assert
        var ex = await Assert.ThrowsAsync<BookingNotFoundException>(
        async () => await _service.GetBookingByIdAsync(bookingId, default));

        Assert.Contains(bookingId.ToString(), ex.Message);
    }
}