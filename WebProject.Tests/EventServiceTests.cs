using Moq;
using System.Linq.Expressions;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _mockRepository;
    private readonly IEventService _service;

    public EventServiceTests()
    {
        _mockRepository = new Mock<IEventRepository>();
        _service = new EventService(_mockRepository.Object);
    }

    // Успешные сценарии:

    // 1. создание события
    [Fact]
    public void CreateEvent_Succeeds()
    {
        // Arrange
        var eventRequest = new EventDto
        {
            Title = "TitleString",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };

        var eventResult = new Event
        {
            Id = 1,
            Title = "TitleString",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };
        _mockRepository.Setup(m => m.AddEvent(It.IsAny<EventDto>())).Returns(eventResult);

        // Act
        var result = _service.AddEvent(eventRequest);

        // Assert
        _mockRepository.Verify(r => r.AddEvent(It.IsAny<EventDto>()), Times.Once);

        Assert.Equal(eventRequest.Title, result.Title);
    }

    // 2. получение всех событий
    [Fact]
    public void GetAllEvents_WithoutFilters_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            new Event
            {
                Id = 1,
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = 2,
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = 3,
                Title = "Event After Range",
                StartAt = new DateTime(2026, 4, 20),
                EndAt = new DateTime(2026, 4, 22)
            }
        };

        _mockRepository.Setup(m => m.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>())).Returns((testEvents.ToList()));

        // Act
        var result = _service.GetAllEvents();

        // Assert
        _mockRepository.Verify(r => r.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>()), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.Equal(3, result.TotalCount);
    }

    // 3. получение события по ID
    [Fact]
    public void GetEventById_Succeeds()
    {
        // Arrange
        var eventResult = new Event
        {
            Id = 1,
            Title = "TitleString",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };
        _mockRepository.Setup(m => m.GetEvent(It.IsAny<int>())).Returns(eventResult);

        // Act
        var result = _service.GetEvent(1);

        // Assert
        Assert.IsAssignableFrom<Event>(result);
        Assert.Equal(eventResult.Title, result.Title);
    }

    // 4. обновление существующего события
    [Fact]
    public void UpdateExistingEvent_Succeeds()
    {
        // Arrange
        var existsEvent = new Event
        {
            Id = 1,
            Title = "TitleString",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };
        var newEventData = new EventDto
        {
            Title = "UpdatedTitle",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };
        _mockRepository.Setup(m => m.GetEvent(It.Is<int>(id => id == 1))).Returns(existsEvent);
        _mockRepository.Setup(m => m.UpdateEvent(It.IsAny<EventDto>(), It.Is<int>(id => id == 1))).Returns(true);

        // Act
        _service.UpdateEvent(newEventData, 1);

        // Assert
        _mockRepository.Verify(m => m.UpdateEvent(It.Is<EventDto>(dto =>
            dto.Title == "UpdatedTitle" &&
            dto.StartAt == DateTime.Parse("2026-04-10") &&
            dto.EndAt == DateTime.Parse("2026-04-11")),
        It.Is<int>(id => id == 1)),
        Times.Once);
    }

    // 5. удаление существующего события
    [Fact]
    public void DeleteExistingEvent_Succeeds()
    {
        // Arrange
        var existsEvent = new Event
        {
            Id = 1,
            Title = "TitleString",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };
        _mockRepository.Setup(m => m.GetEvent(It.Is<int>(id => id == 1))).Returns(existsEvent);
        _mockRepository.Setup(m => m.DeleteEvent(It.IsAny<int>())).Returns(true);

        // Act
        _service.DeleteEvent(1);

        // Assert
        _mockRepository.Verify(m => m.DeleteEvent(
        It.Is<int>(id => id == 1)),
        Times.Once);
    }

    // 6. фильтрация по названию
    [Fact]
    public void GetEvents_FilteringByTitle_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            new Event
            {
                Id = 1,
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = 2,
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = 3,
                Title = "Event After Range",
                StartAt = new DateTime(2026, 4, 20),
                EndAt = new DateTime(2026, 4, 22)
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

        _mockRepository.Setup(m => m.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>())).Returns((testEvents.AsQueryable().Where(predicate).ToList()));

        // Act
        var result = _service.GetAllEvents(title: title);

        // Assert
        _mockRepository.Verify(r => r.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>()), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.Single(result.Items);
        Assert.Contains(title, result.Items.First().Title);
    }

    // 7. фильтрация по датам(startDate < endDate)
    [Fact]
    public void GetEvents_FilteringBy_StartDat_EndDate_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            new Event
            {
                Id = 1,
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = 2,
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = 3,
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

        _mockRepository.Setup(m => m.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>())).Returns((testEvents.AsQueryable().Where(predicate).ToList()));


        // Act
        var result = _service.GetAllEvents(from: from, to: to);

        // Assert
        _mockRepository.Verify(r => r.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>()), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.Single(result.Items);
        Assert.Equal("Event In Range", result.Items.First().Title);
    }

    // 8. пагинация событий
    [Fact]
    public void GetEvents_WithPaging_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            new Event
            {
                Id = 1,
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = 2,
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = 3,
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

        _mockRepository.Setup(m => m.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>())).Returns((testEvents.AsQueryable().Where(predicate).ToList()));

        // Act
        var result = _service.GetAllEvents(page: 2, pageSize: 2);

        // Assert
        _mockRepository.Verify(r => r.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>()), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasPreviousPage);
    }

    // 9. комбинированная фильтрация
    [Fact]
    public void GetEvents_FilteringByAllParams_Succeeds()
    {
        // Arrange
        var testEvents = new List<Event>
        {
            new Event
            {
                Id = 1,
                Title = "Event In Range",
                StartAt = new DateTime(2026, 4, 10),
                EndAt = new DateTime(2026, 4, 12)
            },
            new Event
            {
                Id = 2,
                Title = "Event Before Range",
                StartAt = new DateTime(2026, 4, 5),
                EndAt = new DateTime(2026, 4, 6)
            },
            new Event
            {
                Id = 3,
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

        _mockRepository.Setup(m => m.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>())).Returns((testEvents.AsQueryable().Where(predicate).ToList()));

        // Act
        var result = _service.GetAllEvents(page: 1, pageSize: 2, title: title, from: from, to: to);

        // Assert
        _mockRepository.Verify(r => r.GetAllEvents(It.IsAny<Expression<Func<Event, bool>>>()), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
        Assert.Single(result.Items);
        Assert.Contains(title, result.Items.First().Title);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasNextPage);
    }

    // Неуспешные сценарии:

    // 10. попытка получить событие с несуществующим ID
    [Fact]
    public void GetEventById_nonExistentevent_ThrowsEventNotFoundException()
    {
        // Arrange
        int id = 999;
        _mockRepository.Setup(m => m.GetEvent(It.IsAny<int>())).Returns((Event)null);

        // Assert
        var ex = Assert.Throws<EventNotFoundException>(
        () => _service.GetEvent(999));

        Assert.Contains("999", ex.Message);

        _mockRepository.Verify(m => m.GetEvent(id), Times.Once);
    }

    // 11. попытка обновить событие с несуществующим ID
    [Fact]
    public void UpdateEventById_nonExistentevent_ThrowsEventNotFoundException()
    {
        // Arrange
        var newEventData = new EventDto
        {
            Title = "UpdatedTitle",
            StartAt = DateTime.Parse("2026-04-10"),
            EndAt = DateTime.Parse("2026-04-11")
        };

        int id = 999;
        _mockRepository.Setup(m => m.GetEvent(It.IsAny<int>())).Returns((Event)null);
        _mockRepository.Setup(m => m.UpdateEvent(It.IsAny<EventDto>(), It.IsAny<int>())).Returns(true);

        // Assert
        var ex = Assert.Throws<EventNotFoundException>(
        () => _service.GetEvent(999));

        Assert.Contains("999", ex.Message);

        _mockRepository.Verify(m => m.UpdateEvent(newEventData, id), Times.Never);
    }

    // 12. создание события с некорректными данными(если валидация в сервисе)
    [Fact]
    public void CreateEvent_InvalidEventData_ValidationException()
    {
        // Arrange
        var eventRequest = new EventDto
        {
            Title = ""
        };

        _mockRepository.Setup(m => m.AddEvent(It.IsAny<EventDto>())).Returns((Event)null);

        // Assert
        var ex = Assert.Throws<ValidationException>(
        () => _service.AddEvent(eventRequest));

        Assert.Equal(3, ex.Errors.Count);

        _mockRepository.Verify(m => m.AddEvent(eventRequest), Times.Never);
    }

    // 13. обновление события с некорректными датами(EndAt раньше StartAt)
    [Fact]
    public void CreateEvent_EndAtlessStartAt_ValidationException()
    {
        // Arrange
        var eventRequest = new EventDto
        {
            Title = "TitleString",
            StartAt = DateTime.Parse("2026-04-11"),
            EndAt = DateTime.Parse("2026-04-10")
        };

        _mockRepository.Setup(m => m.AddEvent(It.IsAny<EventDto>())).Returns((Event)null);

        // Assert
        var ex = Assert.Throws<ValidationException>(
        () => _service.AddEvent(eventRequest));

        Assert.Contains("Failed data validation", ex.Message);

        _mockRepository.Verify(m => m.AddEvent(eventRequest), Times.Never);
    }
}