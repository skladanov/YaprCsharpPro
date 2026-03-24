using Moq;

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
        _mockRepository.Setup(m => m.GetAllEvents(null, null, null)).Returns(new List<Event>());

        // Act
        var result = _service.GetAllEvents();

        // Assert
        _mockRepository.Verify(r => r.GetAllEvents(null, null, null), Times.Once);
        Assert.IsAssignableFrom<PaginatedResult<Event>>(result);
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
        _mockRepository.Setup(m => m.GetEvent(It.IsAny<int>())).Returns(existsEvent);
        _mockRepository.Setup(m => m.UpdateEvent(It.IsAny<EventDto>(), It.IsAny<int>())).Returns(true);

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

        // Act

        // Assert
    }

    // 6. фильтрация по названию
    [Fact]
    public void GetEvents_FilteringByTitle_Succeeds()
    {
        // Arrange

        // Act

        // Assert
    }

    // 7. фильтрация по датам(startDate, endDate)
    [Fact]
    public void GetEvents_FilteringBy_StartDat_EndDate_Succeeds()
    {
        // Arrange

        // Act

        // Assert
    }

    // 8. пагинация событий
    [Fact]
    public void GetEvents_WithPaging_Succeeds()
    {
        // Arrange

        // Act

        // Assert
    }

    // 9. комбинированная фильтрация
    [Fact]
    public void GetEvents_FilteringByAllParams_Succeeds()
    {
        // Arrange

        // Act

        // Assert
    }

    // Неуспешные сценарии:

    // 10. попытка получить событие с несуществующим ID
    [Fact]
    public void GetEventById_nonExistentevent_ThrowsEventNotFoundException()
    {
        // Arrange

        // Act

        // Assert
    }
    // 11. попытка обновить событие с несуществующим ID
    [Fact]
    public void UpdateEventById_nonExistentevent_ThrowsEventNotFoundException()
    {
        // Arrange

        // Act

        // Assert
    }

    // 12. создание события с некорректными данными(если валидация в сервисе)
    [Fact]
    public void CreateEvent_InvalidEventData_ValidationException()
    {
        // Arrange

        // Act

        // Assert
    }

    // 13. обновление события с некорректными датами(EndAt раньше StartAt)
    [Fact]
    public void CreateEvent_EndAtlessStartAt_ValidationException()
    {
        // Arrange

        // Act

        // Assert
    }
}
