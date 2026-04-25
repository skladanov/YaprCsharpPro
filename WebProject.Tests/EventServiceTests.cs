using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _mockRepository;
    private readonly Mock<ILogger<EventService>> _mockLogger;
    private readonly IEventService _service;

    public EventServiceTests()
    {
        _mockRepository = new Mock<IEventRepository>();
        _mockLogger = new Mock<ILogger<EventService>>();
        _service = new EventService(_mockRepository.Object, _mockLogger.Object);
    }

    // Успешные сценарии:

    // 1. создание события
    [Fact]
    public async Task CreateEvent_Succeeds()
    {
        // Arrange
        var eventRequest = new CreateEvent
        {
            Title = "TitleString",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now + TimeSpan.FromDays(1)
        };

        var resultEventId = Guid.NewGuid();

        _mockRepository
            .Setup(m => m.AddEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

        // Act
        var result = await _service.AddEventAsync(eventRequest, default);

        // Assert
        _mockRepository.Verify(r => r.AddEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()),Times.Once);

        Assert.IsType<Guid>(result);
    }

    // 2. получение всех событий
    [Fact]
    public async Task GetAllEvents_WithoutFilters_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            Event.Create( 
                Guid.NewGuid(),
                "Event In Range",
                new DateTime(2026, 4, 10),
                new DateTime(2026, 4, 12),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event Before Range",
                new DateTime(2026, 4, 5),
                new DateTime(2026, 4, 6),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event After Range",
                new DateTime(2026, 4, 20),
                new DateTime(2026, 4, 22),
                10
            )
        };

        _mockRepository.Setup(m => m.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default)).ReturnsAsync((testEvents.ToList()));

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        _mockRepository.Verify(r => r.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.Equal(3, result.TotalCount);
    }

    // 3. получение события по ID
    [Fact]
    public async Task GetEventById_Succeeds()
    {
        // Arrange
        Event eventResult = Event.Create(
            Guid.NewGuid(),
            "Title",
            DateTime.Now,
            DateTime.Now.AddDays(1),
            10
        );

        _mockRepository.Setup(m => m.GetEventAsync(It.IsAny<Guid>(), default)).ReturnsAsync(eventResult);

        // Act
        var result = await _service.GetEventAsync(eventResult.Id, default);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Event>(result);
        Assert.Equal(eventResult.Title, result.Title);
    }

    // 4. обновление существующего события
    [Fact]
    public async Task UpdateExistingEvent_Succeeds()
    {
        // Arrange

        var id = Guid.NewGuid();

        Event existsEvent = Event.Create(
            id,
            "Title",
            DateTime.Now,
            DateTime.Now.AddDays(1),
            10
        );

        var newEventData = new UpdateEvent
        {
            Title = "UpdatedTitle",
            StartAt = DateTime.Now.AddDays(2),
            EndAt = DateTime.Now.AddDays(4)
        };


        _mockRepository.Setup(m => m.UpdateEventAsync(It.IsAny<Event>(), default)).ReturnsAsync(true);

        // Act
        await _service.UpdateEventAsync(newEventData, existsEvent.Id, default);

        // Assert
        _mockRepository.Verify(m => m.UpdateEventAsync(It.IsAny<Event>(), default), Times.Once);
    }

    // 5. удаление существующего события
    [Fact]
    public async Task DeleteExistingEvent_Succeeds()
    {
        // Arrange
        Event existsEvent = Event.Create(
            Guid.NewGuid(),
            "Title",
            DateTime.Now,
            DateTime.Now.AddDays(1),
            10
        );

        _mockRepository.Setup(m => m.DeleteEventAsync(It.IsAny<Guid>(), default)).ReturnsAsync(true);

        // Act
        await _service.DeleteEventAsync(existsEvent.Id, default);

        // Assert
        _mockRepository.Verify(m => m.DeleteEventAsync(It.Is<Guid>(id => id == existsEvent.Id), default), Times.Once);
    }

    // 6. фильтрация по названию
    [Fact]
    public async Task GetEvents_FilteringByTitle_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            Event.Create(
                Guid.NewGuid(),
                "Event In Range",
                new DateTime(2026, 4, 10),
                new DateTime(2026, 4, 12),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event Before Range",
                new DateTime(2026, 4, 5),
                new DateTime(2026, 4, 6),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event After Range",
                new DateTime(2026, 4, 20),
                new DateTime(2026, 4, 22),
                10
            )
        };

        string title = "Event In";
        DateTime? from = null;
        DateTime? to = null;

        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        _mockRepository.Setup(m => m.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default)).ReturnsAsync((testEvents.AsQueryable().Where(predicate).ToList()));

        // Act
        var result = await _service.GetAllEventsAsync(title: title);

        // Assert
        _mockRepository.Verify(r => r.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Contains(title, result.Items.First().Title);
    }

    // 7. фильтрация по датам(startDate < endDate)
    [Fact]
    public async Task GetEvents_FilteringBy_StartDat_EndDate_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            Event.Create(
                Guid.NewGuid(),
                "Event In Range",
                new DateTime(2026, 4, 10),
                new DateTime(2026, 4, 12),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event Before Range",
                new DateTime(2026, 4, 5),
                new DateTime(2026, 4, 6),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event After Range",
                new DateTime(2026, 4, 20),
                new DateTime(2026, 4, 22),
                10
            )
        };

        string title = "Event In";
        DateTime? from = new DateTime(2026, 4, 9);
        DateTime? to = new DateTime(2026, 4, 13);

        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        _mockRepository.Setup(m => m.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default)).ReturnsAsync((testEvents.AsQueryable().Where(predicate).ToList()));


        // Act
        var result = await _service.GetAllEventsAsync(from: from, to: to);

        // Assert
        _mockRepository.Verify(r => r.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Equal("Event In Range", result.Items.First().Title);
    }

    // 8. пагинация событий
    [Fact]
    public async Task GetEvents_WithPaging_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            Event.Create(
                Guid.NewGuid(),
                "Event In Range",
                new DateTime(2026, 4, 10),
                new DateTime(2026, 4, 12),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event Before Range",
                new DateTime(2026, 4, 5),
                new DateTime(2026, 4, 6),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event After Range",
                new DateTime(2026, 4, 20),
                new DateTime(2026, 4, 22),
                10
            )
        };

        string? title = null;
        DateTime? from = null;
        DateTime? to = null;

        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        _mockRepository.Setup(m => m.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default)).ReturnsAsync((testEvents.AsQueryable().Where(predicate).ToList()));

        // Act
        var result = await _service.GetAllEventsAsync(page: 2, pageSize: 2);

        // Assert
        _mockRepository.Verify(r => r.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasPreviousPage);
    }

    // 9. комбинированная фильтрация
    [Fact]
    public async Task GetEvents_FilteringByAllParams_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            Event.Create(
                Guid.NewGuid(),
                "Event In Range",
                new DateTime(2026, 4, 10),
                new DateTime(2026, 4, 12),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event Before Range",
                new DateTime(2026, 4, 5),
                new DateTime(2026, 4, 6),
                10
            ),
            Event.Create(
                Guid.NewGuid(),
                "Event After Range",
                new DateTime(2026, 4, 20),
                new DateTime(2026, 4, 22),
                10
            )
        };

        string title = "Event In";
        DateTime? from = new DateTime(2026, 4, 8);
        DateTime? to = new DateTime(2026, 4, 15);

        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        _mockRepository.Setup(m => m.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default)).ReturnsAsync((testEvents.AsQueryable().Where(predicate).ToList()));

        // Act
        var result = await _service.GetAllEventsAsync(page: 1, pageSize: 2, title: title, from: from, to: to);

        // Assert
        _mockRepository.Verify(r => r.GetAllEventsAsync(It.IsAny<Expression<Func<Event, bool>>>(), default), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Contains(title, result.Items.First().Title);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasNextPage);
    }

    // Неуспешные сценарии:

    // 10. попытка получить событие с несуществующим ID
    [Fact]
    public async Task GetEventById_nonExistentevent_ThrowsEventNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mockRepository.Setup(m => m.GetEventAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Event?)null);

        // Assert
        var ex = await Assert.ThrowsAsync<EventNotFoundException>(
            async () => await _service.GetEventAsync(id, default));

        Assert.Contains(id.ToString(), ex.Message);

        _mockRepository.Verify(m => m.GetEventAsync(id, default), Times.Once);
    }

    // 11. попытка обновить событие с несуществующим ID
    [Fact]
    public async Task UpdateEventById_nonExistentevent_ThrowsEventNotFoundException()
    {
        // Arrange
        var newEventData = new CreateEvent
        {
            Title = "UpdatedTitle",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };

        var id = Guid.NewGuid();

        _mockRepository.Setup(m => m.UpdateEventAsync(It.IsAny<Event>(), default));

        // Assert
        var ex = await Assert.ThrowsAsync<EventNotFoundException>(
        async () => await _service.GetEventAsync(id, default));

        Assert.Contains(id.ToString(), ex.Message);

        _mockRepository.Verify(m => m.UpdateEventAsync(It.IsAny<Event>(), default), Times.Never);
    }

    // 12. создание события с некорректными данными(если валидация в сервисе)
    [Fact]
    public async Task CreateEvent_InvalidEventData_ValidationException()
    {
        // Arrange
        var eventRequest = new CreateEvent
        {
            Title = ""
        };

        _mockRepository.Setup(m => m.AddEventAsync(It.IsAny<Event>(), default));

        // Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
        async () => await _service.AddEventAsync(eventRequest, default));

        _mockRepository.Verify(m => m.AddEventAsync(It.IsAny<Event>(), default), Times.Never);
    }

    // 13. обновление события с некорректными датами(EndAt раньше StartAt)
    [Fact]
    public async Task CreateEvent_EndAtlessStartAt_ValidationException()
    {
        // Arrange
        var eventRequest = new CreateEvent
        {
            Title = "TitleString",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now - TimeSpan.FromDays(1)
        };

        _mockRepository.Setup(m => m.AddEventAsync(It.IsAny<Event>(), default));

        // Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
        async () => await _service.AddEventAsync(eventRequest, default));

        Assert.Contains("Failed data validation", ex.Message);

        _mockRepository.Verify(m => m.AddEventAsync(It.IsAny<Event>(), default), Times.Never);
    }
}