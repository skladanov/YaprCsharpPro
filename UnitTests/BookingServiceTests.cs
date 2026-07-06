using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using static Booking;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _mockBookingRepository;
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<ILogger<BookingService>> _mockBookingLogger;
    private readonly IBookingService _service;

    public BookingServiceTests()
    {
        _mockBookingRepository = new Mock<IBookingRepository>();
        _mockEventRepository = new Mock<IEventRepository>();
        _mockBookingLogger = new Mock<ILogger<BookingService>>();
        _service = new BookingService(_mockBookingRepository.Object, _mockEventRepository.Object, _mockBookingLogger.Object);
    }
    //Успешные сценарии:

    //создание брони для существующего события — возвращается BookingInfo со статусом Pending;
    //Создание брони уменьшает AvailableSeats на 1.
    [Fact]
    public async Task CreateBooking_ForExistingEvent_ReturnsPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );

        int? availableSeatsBefore = mockEvent.AvailableSeats;

        Booking? capturedBooking = null;

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);
        _mockEventRepository.Setup(s => s.UpdateEventAsync(mockEvent, default));
        _mockBookingRepository.Setup(r => r.CreateBookingAsync(It.IsAny<Booking>(), default))
            .Callback<Booking, CancellationToken>((booking, _) =>
            {
                capturedBooking = booking;
            });

        // Act
        var result = await _service.CreateBookingAsync(eventId, default);

        // Assert
        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default),Times.Once());
        _mockEventRepository.Verify(s => s.UpdateEventAsync(mockEvent, default), Times.Once());
        _mockBookingRepository.Verify(r => r.CreateBookingAsync(It.IsAny<Booking>(), default), Times.Once());    

        Assert.Equal(capturedBooking.Id, result);
        Assert.Equal(capturedBooking?.Status, Booking.BookingStatus.Pending);
        Assert.True(availableSeatsBefore - mockEvent.AvailableSeats == 1);
    }

    // Создание нескольких броней для одного события — все создаются с уникальными Id;
    // Создание нескольких броней (до лимита) — все успешны, у каждой уникальный Id.
    // После исчерпания мест следующая попытка выбрасывает NoAvailableSeatsException.
    [Fact]
    public async Task CreateSomeBookings_ForExistingEvent_ReturnsBookingsWithOtherIDs()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            2
        );

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);
        _mockEventRepository.Setup(s => s.UpdateEventAsync(mockEvent, default));
        _mockBookingRepository.Setup(r => r.CreateBookingAsync(It.IsAny<Booking>(), default));

        // Act
        var result1 = await _service.CreateBookingAsync(eventId, default);
        var result2 = await _service.CreateBookingAsync(eventId, default);
        var ex = await Assert.ThrowsAsync<NoAvailableSeatsException>(
           async () => await _service.CreateBookingAsync(eventId, default));

        // Assert
        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default), Times.Exactly(3));
        _mockEventRepository.Verify(s => s.UpdateEventAsync(mockEvent, default), Times.Exactly(2));
        _mockBookingRepository.Verify(s => s.CreateBookingAsync(It.IsAny<Booking>(), default), Times.Exactly(2));

        Assert.NotEqual(result1, result2);
    }

    // Получение брони по Id — возвращается корректная информация;
    // Получение брони отражает изменение статуса(после Confirm/Reject);
    // После вызова Confirm() бронь возвращает статус Confirmed и заполненный ProcessedAt
    [Fact]
    public async Task GetBooking_ForExistBooking_ReturnsStatusConfirm()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );

        mockEvent.TryReserveSeats();

        var initialBooking = Booking.Create(bookingId, eventId);
        var mockListPendingBokkings = new List<Booking> { initialBooking };

        _mockBookingRepository.Setup(br => br.GetBookingByIdAsync(bookingId, default)).ReturnsAsync(initialBooking);
        _mockBookingRepository.Setup(br => br.GetBookingsAsync(It.IsAny<Expression < Func<Booking, bool>>> (), default)).ReturnsAsync(mockListPendingBokkings);
        _mockEventRepository.Setup(er => er.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);
        _mockBookingRepository.Setup(br => br.UpdateBookingAsync(It.IsAny<Booking>(), default));
        _mockEventRepository.Setup(er => er.UpdateEventAsync(It.IsAny<Event>(), default));

        // Act 1
        var beforeProcessResult = await _service.GetBookingByIdAsync(bookingId, default);

        // Assert 1
        Assert.NotNull(beforeProcessResult);
        Assert.Equal(BookingStatus.Pending, beforeProcessResult.Status);
        Assert.Null(beforeProcessResult.ProcessedAt);

        // Act 2
        await _service.Confirm(beforeProcessResult, default);

        DateTime now = DateTime.UtcNow;

        var afterProcessResult = await _service.GetBookingByIdAsync(bookingId, default);

        // Assert 2
        Assert.NotNull(afterProcessResult);
        Assert.Equal(BookingStatus.Confirmed, afterProcessResult.Status);
        Assert.NotNull(afterProcessResult.ProcessedAt);
        Assert.True((afterProcessResult.ProcessedAt.Value - now).Duration() <= TimeSpan.FromSeconds(10));
    }


    // После вызова Reject() бронь возвращает статус Rejected и заполненный ProcessedAt.
    // После Reject() ReleaseSeats() количество свободных мест восстанавливается.
    [Fact]
    public async Task GetBooking_ForCanceledBooking_ReturnsStatusReject()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );

        mockEvent.TryReserveSeats();

        var initialBooking = Booking.Create(bookingId, eventId);
        var mockListPendingBokkings = new List<Booking> { initialBooking };

        _mockBookingRepository.Setup(br => br.GetBookingByIdAsync(bookingId, default)).ReturnsAsync(initialBooking);
        _mockBookingRepository.Setup(br => br.GetBookingsAsync(It.IsAny<Expression<Func<Booking, bool>>>(), default)).ReturnsAsync(mockListPendingBokkings);
        _mockEventRepository.Setup(er => er.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);
        _mockBookingRepository.Setup(br => br.UpdateBookingAsync(It.IsAny<Booking>(), default));
        _mockEventRepository.Setup(br => br.UpdateEventAsync(It.IsAny<Event>(), default));

        // Act 1
        var beforeProcessResult = await _service.GetBookingByIdAsync(bookingId, default);

        // Assert 1
        Assert.NotNull(beforeProcessResult);
        Assert.Equal(BookingStatus.Pending, beforeProcessResult.Status);
        Assert.Null(beforeProcessResult.ProcessedAt);
        Assert.True(mockEvent.TotalSeats - mockEvent.AvailableSeats == 1);

        // Act 2
        await _service.Reject(beforeProcessResult, mockEvent);

        DateTime now = DateTime.UtcNow;

        var afterProcessResult = await _service.GetBookingByIdAsync(bookingId, default);

        // Assert 2
        Assert.NotNull(afterProcessResult);
        Assert.Equal(beforeProcessResult, afterProcessResult);
        Assert.Equal(BookingStatus.Rejected, afterProcessResult.Status);
        Assert.Null(afterProcessResult.ProcessedAt);
        Assert.Equal(mockEvent.TotalSeats, mockEvent.AvailableSeats);
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
        _mockEventRepository.Setup(s => s.UpdateEventAsync(It.IsAny<Event>(), default));
        _mockBookingRepository.Setup(r => r.CreateBookingAsync(It.IsAny<Booking>(), default));

        // Assert
        var ex = await Assert.ThrowsAsync<EventNotFoundException>(
            async () => await _service.CreateBookingAsync(eventId, default));

        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default), Times.Once());
        _mockEventRepository.Verify(s => s.UpdateEventAsync(It.IsAny<Event>(), default), Times.Never);
        _mockBookingRepository.Verify(r => r.CreateBookingAsync(It.IsAny<Booking>(), default), Times.Never());
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

    // Бронирование при отсутствии мест → NoAvailableSeatsException
    [Fact]
    public async Task CreateBooking_ForNoAvailableSeatsEvent_ReturnsNoAvailableSeatsException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            1
        );

        mockEvent.TryReserveSeats();

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);
        _mockEventRepository.Setup(s => s.UpdateEventAsync(mockEvent, default));
        _mockBookingRepository.Setup(r => r.CreateBookingAsync(It.IsAny<Booking>(), default));

        // Act
        var ex = await Assert.ThrowsAsync<NoAvailableSeatsException>(
           async () => await _service.CreateBookingAsync(eventId, default));

        // Assert
        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default), Times.Once);
        _mockEventRepository.Verify(s => s.UpdateEventAsync(mockEvent, default), Times.Never);
        _mockBookingRepository.Verify(s => s.CreateBookingAsync(It.IsAny<Booking>(), default), Times.Never);
    }

    // Тесты на конкурентность:

    // Тест на защиту от овербукинга:
    [Fact]
    public async Task TryCreate20Bookings_ForAvailable5Seats_Returns5Bookings()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            5
        );

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);
        _mockEventRepository.Setup(s => s.UpdateEventAsync(mockEvent, default));
        _mockBookingRepository.Setup(r => r.CreateBookingAsync(It.IsAny<Booking>(), default));

        // Act
        // Создаём задачи для конкурентных запросов
        var tasks = Enumerable.Range(0, 20)
            .Select(_ =>
            {
                return Task.Run(async () =>
                {
                    try
                    {
                        var result = await _service.CreateBookingAsync(eventId, default);
                        return (Success: true, Id: result, Exception: (Exception)null);
                    }
                    catch (NoAvailableSeatsException ex)
                    {
                        return (Success: false, Id: Guid.Empty, Exception: ex);
                    }
                });
            })
        .ToArray();

        // Act: выполняем все запросы параллельно
        var results = await Task.WhenAll(tasks);
        var successfulBookings = results.Where(r => r.Success).Count();

        // Assert
        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default), Times.Exactly(20));
        _mockEventRepository.Verify(s => s.UpdateEventAsync(mockEvent, default), Times.Exactly(5));
        _mockBookingRepository.Verify(s => s.CreateBookingAsync(It.IsAny<Booking>(), default), Times.Exactly(5));

        Assert.Equal(20, results.Length);
        Assert.Equal(5, successfulBookings);
    }


    // Тест на уникальность Id при конкурентных запросах
    [Fact]
    public async Task TryCreate10Bookings_ForAvailable10Seats_AllBookingsUnique()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        Event mockEvent = Event.Create(
            eventId,
            "Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10
        );

        _mockEventRepository.Setup(s => s.GetEventAsync(eventId, default)).ReturnsAsync(mockEvent);
        _mockEventRepository.Setup(s => s.UpdateEventAsync(mockEvent, default));
        _mockBookingRepository.Setup(r => r.CreateBookingAsync(It.IsAny<Booking>(), default));

        // Act
        // Создаём и запускаем конкурентные запросы
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _service.CreateBookingAsync(eventId, default))
            .ToArray();

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert: проверяем уникальность ID
        var uniqueIds = results.Distinct().ToList();

        // Assert
        _mockEventRepository.Verify(s => s.GetEventAsync(eventId, default), Times.Exactly(10));
        _mockEventRepository.Verify(s => s.UpdateEventAsync(mockEvent, default), Times.Exactly(10));
        _mockBookingRepository.Verify(s => s.CreateBookingAsync(It.IsAny<Booking>(), default), Times.Exactly(10));

        Assert.Equal(10, uniqueIds.Count);
    }
}