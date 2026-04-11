using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _mockRepository;
    private readonly Mock<ILogger> _mockLogger;
    private readonly IEventService _service;

    public EventServiceTests()
    {
        _mockRepository = new Mock<IEventRepository>();
        _mockLogger = new Mock<ILogger>();
        _service = new EventService(_mockRepository.Object, _mockLogger.Object);
    }

    // Успешные сценарии:

    // 1. создание события
    [Fact]
    public async Task CreateEvent_Succeeds()
    {
        // Arrange
        var eventRequest = new EventDto
        {
            Title = "TitleString",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now + TimeSpan.FromDays(1)
        };

        var resultEventId = Guid.NewGuid();

        _mockRepository
            .Setup(m => m.AddEventAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultEventId);

        // Act
        var result = await _service.AddEventAsync(eventRequest, default);

        // Assert
        _mockRepository.Verify(
            r => r.AddEventAsync(
                It.IsAny<EventDto>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        Assert.Equal(resultEventId, result);
    }

    // 2. получение всех событий
    [Fact]
    public async Task GetAllEvents_WithoutFilters_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event After Range",
                StartAt = new DateTime(2026, 4, 20),
                EndAt = new DateTime(2026, 4, 22)
            }
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
        var eventResult = new Event
        {
            Id = Guid.NewGuid(),
            Title = "TitleString",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(1)
        };
        _mockRepository.Setup(m => m.GetEventAsync(It.IsAny<Guid>(), default)).ReturnsAsync(eventResult);

        // Act
        var result = await _service.GetEventAsync(eventResult.Id, default);

        // Assert
        Assert.IsAssignableFrom<Event>(result);
        Assert.Equal(eventResult.Title, result.Title);
    }

    // 4. обновление существующего события
    [Fact]
    public async Task UpdateExistingEvent_Succeeds()
    {
        // Arrange
        var existsEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "TitleString",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(2)
        };

        var newEventData = new EventDto
        {
            Title = "UpdatedTitle",
            StartAt = DateTime.Now.AddDays(2),
            EndAt = DateTime.Now.AddDays(4)
        };
        _mockRepository.Setup(m => m.GetEventAsync(It.IsAny<Guid>(), default)).ReturnsAsync(existsEvent);

        _mockRepository.Setup(m => m.UpdateEventAsync(It.IsAny<EventDto>(), It.Is<Guid>(id => id == existsEvent.Id), default));

        // Act
        await _service.UpdateEventAsync(newEventData, existsEvent.Id, default);

        // Assert
        _mockRepository.Verify(m => m.UpdateEventAsync(newEventData, It.Is<Guid>(id => id == existsEvent.Id), default), Times.Once);
    }

    // 5. удаление существующего события
    [Fact]
    public async Task DeleteExistingEvent_Succeeds()
    {
        // Arrange
        var existsEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "TitleString",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(2)
        };

        _mockRepository.Setup(m => m.GetEventAsync(It.IsAny<Guid>(), default)).ReturnsAsync(existsEvent);

        _mockRepository.Setup(m => m.DeleteEventAsync(It.IsAny<Guid>(), default));

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
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event In Range",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(2),
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event Before Range",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event After Range",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            }
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
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event After Range",
                StartAt = new DateTime(2026, 4, 20),
                EndAt = new DateTime(2026, 4, 22)
            }
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
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event After Range",
                StartAt = new DateTime(2026, 4, 20),
                EndAt = new DateTime(2026, 4, 22)
            }
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
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Event After Range",
                StartAt = new DateTime(2026, 4, 20),
                EndAt = new DateTime(2026, 4, 22)
            }
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

        _mockRepository.Setup(m => m.GetEventAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Event)null);

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
        var newEventData = new EventDto
        {
            Title = "UpdatedTitle",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };

        var id = Guid.NewGuid();

        _mockRepository.Setup(m => m.UpdateEventAsync(It.IsAny<EventDto>(), It.IsAny<Guid>(), default));

        // Assert
        var ex = await Assert.ThrowsAsync<EventNotFoundException>(
        async () => await _service.GetEventAsync(id, default));

        Assert.Contains(id.ToString(), ex.Message);

        _mockRepository.Verify(m => m.UpdateEventAsync(newEventData, id, default), Times.Never);
    }

    // 12. создание события с некорректными данными(если валидация в сервисе)
    [Fact]
    public async void CreateEvent_InvalidEventData_ValidationException()
    {
        // Arrange
        var eventRequest = new EventDto
        {
            Title = ""
        };

        _mockRepository.Setup(m => m.AddEventAsync(It.IsAny<EventDto>(), default));

        // Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
        async () => await _service.AddEventAsync(eventRequest, default));

        Assert.Equal(3, ex.Errors.Count);

        _mockRepository.Verify(m => m.AddEventAsync(eventRequest, default), Times.Never);
    }

    // 13. обновление события с некорректными датами(EndAt раньше StartAt)
    [Fact]
    public async void CreateEvent_EndAtlessStartAt_ValidationException()
    {
        // Arrange
        var eventRequest = new EventDto
        {
            Title = "TitleString",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now - TimeSpan.FromDays(1)
        };

        _mockRepository.Setup(m => m.AddEventAsync(It.IsAny<EventDto>(), default));

        // Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
        async () => await _service.AddEventAsync(eventRequest, default));

        Assert.Contains("Failed data validation", ex.Message);

        _mockRepository.Verify(m => m.AddEventAsync(eventRequest, default), Times.Never);
    }
}