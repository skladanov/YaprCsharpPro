using Microsoft.Extensions.Logging;
using Moq;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repoMock;
    private readonly Mock<IEventCacheRepository> _cacheMock;
    private readonly Mock<ILogger<EventService>> _logMock;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _repoMock = new Mock<IEventRepository>();
        _cacheMock = new Mock<IEventCacheRepository>();
        _logMock = new Mock<ILogger<EventService>>();
        _service = new EventService(_repoMock.Object, _cacheMock.Object, _logMock.Object);
    }

    /// <summary>
    /// Сценарий 1: При попадании в кеш репозиторий НЕ вызывается.
    /// </summary>
    [Fact]
    public async Task GetTop10BySalesPercentage_WhenCacheHit_ShouldNotCallRepository()
    {
        // Arrange
        var cachedEvents = new List<Event>();

        for (int i = 1; i <= 10; i++)
        {
            var totalSeats = 15;

            var @event = Event.Create(

                id: Guid.NewGuid(),
                title: $"Event {i}",
                description: $"Описание события {i}.",
                startAt: DateTime.UtcNow.AddDays(i),
                endAt: DateTime.UtcNow.AddDays(i + 2),
                totalSeats: totalSeats
            );

            @event.TryReserveSeats(totalSeats - i);

            cachedEvents.Add(@event);
        }

        _cacheMock.Setup(c => c.GetTop10PopularEventsAsync()).ReturnsAsync(cachedEvents);

        // Act
        var result = await _service.GetTop10PopularAsync(CancellationToken.None);

        // Assert
        Assert.Equal(cachedEvents, result);
        _repoMock.Verify(r => r.GetTop10BySalesPercentageAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.GetTop10PopularEventsAsync(), Times.Once);
    }

    /// <summary>
    /// Сценарий 2: При промахе кеш заполняется данными из репозитория.
    /// </summary>
    [Fact]
    public async Task GetTop10BySalesPercentage_WhenCacheMiss_ShouldCallRepositoryAndSetCache()
    {
        // Arrange
        var repoEvents = new List<Event>();

        for (int i = 1; i <= 15; i++)
        {
            var totalSeats = 20;

            var @event = Event.Create(
            
                id: Guid.NewGuid(),
                title: $"Event {i}",
                description: $"Описание события {i}.",
                startAt: DateTime.UtcNow.AddDays(i),
                endAt: DateTime.UtcNow.AddDays(i + 2),
                totalSeats: totalSeats
            );

            @event.TryReserveSeats(totalSeats - i);

            repoEvents.Add( @event );
        }

        _cacheMock.Setup(c => c.GetTop10PopularEventsAsync()).ReturnsAsync(new List<Event>()); // пустой список = промах

        _repoMock.Setup(r => r.GetTop10BySalesPercentageAsync(It.IsAny<CancellationToken>())).ReturnsAsync(repoEvents);

        var ttl = TimeSpan.FromMinutes(5);

        // Act
        var result = await _service.GetTop10PopularAsync(CancellationToken.None);

        // Assert
        Assert.Equal("Event 1", result.FirstOrDefault().Title); // Проверяем, что Event 1 первый в топе

        _repoMock.Verify(r => r.GetTop10BySalesPercentageAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Сценарий 3: При обновлении события (если меняются места) кеш топ‑10 инвалидируется.
    /// </summary>
    [Fact]
    public async Task UpdateEvent_WhenSeatsChanged_ShouldInvalidateTop10Cache()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = Event.Create(
            id: eventId,
            title: "Old Title",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 100
        );

        var updateEvent = new UpdateEvent
        {
            Title = existingEvent.Title,
            TotalSeats = 120
        };

        _repoMock.Setup(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(existingEvent);

        // Act
        await _service.UpdateEventAsync(updateEvent, eventId, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);

        // Кэш события по ID должен быть удалён
        _cacheMock.Verify(c => c.InvalidateEventByIdAsync(eventId), Times.Once);
    }

    /// <summary>
    /// Сценарий 5: При удалении события кеш по ID и топ‑10 инвалидируются.
    /// </summary>
    [Fact]
    public async Task DeleteEvent_ShouldInvalidateCacheByIdAndTop10()
    {
        var existEvent = Event.Create(
            id: Guid.NewGuid(),
            title: "Title",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 100
        );

        _repoMock.Setup(r => r.GetEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(existEvent);
        _repoMock
            .Setup(r => r.DeleteEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteEventAsync(existEvent.Id, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.DeleteEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);

        // Инвалидируем кэш по ID
        _cacheMock.Verify(c => c.InvalidateEventByIdAsync(It.IsAny<Guid>()), Times.Once);
    }
}
