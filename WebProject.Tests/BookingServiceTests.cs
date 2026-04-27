using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Reflection;
using static Booking;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _mockBookingRepository;
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<ILogger<BookingService>> _mockBookingLogger;
    private readonly Mock<ILogger<BookingProcessService>> _mockProcessLogger;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly IBookingService _service;
    private readonly BookingProcessService _processService;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public BookingServiceTests()
    {
        _mockBookingRepository = new Mock<IBookingRepository>();
        _mockEventRepository = new Mock<IEventRepository>();
        _mockBookingLogger = new Mock<ILogger<BookingService>>();
        _mockProcessLogger = new Mock<ILogger<BookingProcessService>>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _service = new BookingService(_mockBookingRepository.Object, _mockEventRepository.Object, _mockBookingLogger.Object);
        _processService = new BookingProcessService(_mockScopeFactory.Object, _mockProcessLogger.Object);
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

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);

        _mockBookingRepository.Setup(r => r.CreateBookingAsync(eventId, default)).ReturnsAsync(resultBookingId);

        // Act
        var result = await _service.CreateBookingAsync(eventId, default);

        // Assert
        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default),Times.Once());
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

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);

        _mockBookingRepository.Setup(r => r.CreateBookingAsync(eventId, default)).ReturnsAsync(() => Guid.NewGuid());

        // Act
        var result1 = await _service.CreateBookingAsync(eventId, default);
        var result2 = await _service.CreateBookingAsync(eventId, default);

        // Assert
        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default), Times.AtMost(3));
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

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.Now,
            DateTime.Now.AddDays(1),
            10
        );

        var initialBooking = Booking.Create(bookingId, eventId);

        var mockListPendingBokkings = new List<Booking> { initialBooking };

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);

        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IEventRepository))).Returns(_mockEventRepository.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IBookingRepository))).Returns(_mockBookingRepository.Object);

        _mockBookingRepository.Setup(br => br.GetBookingByIdAsync(bookingId, default)).ReturnsAsync(initialBooking);

        _mockBookingRepository.Setup(br => br.GetBookingsAsync(It.IsAny<Expression < Func<Booking, bool>>> (), default)).ReturnsAsync(mockListPendingBokkings);

        _mockEventRepository.Setup(er => er.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);

        // Act 1
        var firstResult = await _service.GetBookingByIdAsync(bookingId, default);

        // Assert 1
        Assert.NotNull(firstResult);
        Assert.Equal(BookingStatus.Pending, firstResult.Status);
        Assert.Null(firstResult.ProcessedAt);

        // Act 2
        await _processService.ExecuteBookingProcessForTestsAsync(CancellationToken.None);
        DateTime now = DateTime.Now;
        await Task.Delay(5000);

        var secondResult = await _service.GetBookingByIdAsync(bookingId, default);


        // Assert 2
        Assert.NotNull(secondResult);
        Assert.Equal(BookingStatus.Confirmed, secondResult.Status);
        Assert.NotNull(secondResult.ProcessedAt);
        Assert.True((secondResult.ProcessedAt.Value - now).Duration() <= TimeSpan.FromSeconds(3));
    }


    //Неуспешные сценарии:
    //создание брони для несуществующего события;
    //создание брони для удалённого события;
    [Fact]
    public async Task CreateBooking_ForNonExistentEvent_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ThrowsAsync(new EventNotFoundException(eventId));

        _mockBookingRepository.Setup(r => r.CreateBookingAsync(eventId, default));


        // Assert
        var ex = await Assert.ThrowsAsync<EventNotFoundException>(
            async () => await _service.CreateBookingAsync(eventId, default));

        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default), Times.Once());
        _mockBookingRepository.Verify(r => r.CreateBookingAsync(eventId, default), Times.Never());
    }


    //получение брони по несуществующему Id
    [Fact]
    public async Task GetBooking_ForNonExistentBooking_Returns404NotFound()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _mockBookingRepository
            .Setup(r => r.GetBookingByIdAsync(bookingId, default))
            .ReturnsAsync((Booking?)null);

        // Assert
        var ex = await Assert.ThrowsAsync<BookingNotFoundException>(
        async () => await _service.GetBookingByIdAsync(bookingId, default));

        Assert.Contains(bookingId.ToString(), ex.Message);
    }
}