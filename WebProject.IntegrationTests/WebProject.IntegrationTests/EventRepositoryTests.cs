using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using WebProject.DataAccess;
using Xunit;

public class EventRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventapi")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();
    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

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
    public async Task GetEventById_ForExistingEvent_ReturnsEvent()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var testEvent = Event.Create(
            Guid.NewGuid(),
            "Event",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        context.events.Add(testEvent);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetEventAsync(testEvent.Id, default);

        // Assert
        await using var verifyContext = CreateContext();
        Assert.NotNull(result);
        Assert.Equal("Event", result.Title);
    }

    [Fact]
    public async Task CreateEvent_ForNonExistingEvent_EventCreated()
    {
        await ResetDatabaseAsync();

        // Arrange
        var testEvent = Event.Create(
            Guid.NewGuid(),
            "Event_2",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );

        // Act
        await using var context = CreateContext();
        var repository = new EventRepository(CreateContext());
        await repository.AddEventAsync(testEvent, default);

        // Assert
        await using var verifyContext = CreateContext();
        var result = verifyContext.events.FirstOrDefault(e => e.Id == testEvent.Id);
        Assert.NotNull(result);
        Assert.Equal("Event_2", result.Title);
    }

    [Fact]
    public async Task UpdateEvent_ForExistingEvent_EventUpdated()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var testEvent = Event.Create(
            Guid.NewGuid(),
            "testEvent",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        context.events.Add(testEvent);
        await context.SaveChangesAsync();

        var updatedEvent = Event.Create(
            testEvent.Id,
            "updatedEvent",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );

        // Act
        var repository = new EventRepository(CreateContext());
        await repository.UpdateEventAsync(updatedEvent, default);

        // Assert
        await using var verifyContext = CreateContext();
        var result = verifyContext.events.FirstOrDefault(e => e.Id == testEvent.Id);
        Assert.NotNull(result);
        Assert.Equal("updatedEvent", result.Title);
    }

    [Fact]
    public async Task DeleteEvent_ForExistingEvent_EventDeleted()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var testEvent = Event.Create(
            Guid.NewGuid(),
            "Event",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        context.events.Add(testEvent);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());
        await repository.DeleteEventAsync(testEvent, default);

        // Assert
        await using var verifyContext = CreateContext();
        var result = verifyContext.events.FirstOrDefault(e => e.Id == testEvent.Id);
        Assert.Null(result);
    }

    // Получение всех событий
    [Fact]
    public async Task GetAllEvents_WithoutFilters_Succeeds()
    {
        // Arrange
        await using var context = CreateContext();
        var mockEvent1 = Event.Create(
            Guid.NewGuid(),
            "Event 1",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );

        var mockEvent2 = Event.Create(
            Guid.NewGuid(),
            "Event 2",
            new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        var mockEvent3 = Event.Create(
            Guid.NewGuid(),
            "Event 3",
            new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
            10
        );

        context.events.Add(mockEvent1);
        context.events.Add(mockEvent2);
        context.events.Add(mockEvent3);
        await context.SaveChangesAsync();

        string title = string.Empty;
        DateTime? from = null;
        DateTime? to = null;

        Expression<Func<Event, bool>> predicate = e =>
            (string.IsNullOrEmpty(title) ||
             e.Title.Contains(title)) &&
            (!from.HasValue || e.StartAt >= from.Value) &&
            (!to.HasValue || e.EndAt <= to.Value);

        // Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetAllEventsAsync(predicate, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        var testedEvent1 = result.FirstOrDefault(e => e.Id == mockEvent1.Id);
        Assert.Equal(mockEvent1.Title, testedEvent1.Title);
        var testedEvent2 = result.FirstOrDefault(e => e.Id == mockEvent2.Id);
        Assert.Equal(mockEvent2.Title, testedEvent2.Title);
        var testedEvent3 = result.FirstOrDefault(e => e.Id == mockEvent3.Id);
        Assert.Equal(mockEvent3.Title, testedEvent3.Title);
    }

    // Фильтрация по названию
    [Fact]
    public async Task GetEvents_FilteringByTitle_Succeeds()
    {
        // Arrange
        await using var context = CreateContext();
        var mockEvent1 = Event.Create(
            Guid.NewGuid(),
            "Event 1",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        var mockEvent2 = Event.Create(
            Guid.NewGuid(),
            "Event 2",
            new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        var mockEvent3 = Event.Create(
            Guid.NewGuid(),
            "Event 3",
            new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
            10
        );

        context.events.Add(mockEvent1);
        context.events.Add(mockEvent2);
        context.events.Add(mockEvent3);
        await context.SaveChangesAsync();

        string title = "Event 2";
        DateTime? from = null;
        DateTime? to = null;

        Expression<Func<Event, bool>> predicate = e =>
            (string.IsNullOrEmpty(title) ||
             e.Title.Contains(title)) &&
            (!from.HasValue || e.StartAt >= from.Value) &&
            (!to.HasValue || e.EndAt <= to.Value);

        // Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetAllEventsAsync(predicate, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var testedEvent = result.FirstOrDefault(e => e.Title == title);
        Assert.Equal(mockEvent2.Id, testedEvent.Id);
    }

    // Фильтрация по дате
    [Fact]
    public async Task GetEvents_FilteringBy_StartDat_EndDate_Succeeds()
    {
        // Arrange
        await using var context = CreateContext();
        var mockEvent1 = Event.Create(
            Guid.NewGuid(),
            "Event 1",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        var mockEvent2 = Event.Create(
            Guid.NewGuid(),
            "Event 2",
            new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        var mockEvent3 = Event.Create(
            Guid.NewGuid(),
            "Event 3",
            new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
            10
        );

        context.events.Add(mockEvent1);
        context.events.Add(mockEvent2);
        context.events.Add(mockEvent3);
        await context.SaveChangesAsync();

        string title = string.Empty;
        DateTime? from = new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc);
        DateTime? to = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc);

        Expression<Func<Event, bool>> predicate = e =>
            (string.IsNullOrEmpty(title) ||
             e.Title.Contains(title)) &&
            (!from.HasValue || e.StartAt >= from.Value) &&
            (!to.HasValue || e.EndAt <= to.Value);

        // Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetAllEventsAsync(predicate, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var testedEvent = result.FirstOrDefault();
        Assert.Equal(mockEvent1.Id, testedEvent.Id);
    }

    // Negative tests
    [Fact]
    public async Task GetEventById_ForNonExistingEvent_ReturnsNull()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var mockEvent = Event.Create(
            Guid.NewGuid(),
            "Event",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            10
        );
        var testID = Guid.NewGuid();
        context.events.Add(mockEvent);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetEventAsync(testID , default);

        // Assert
        await using var verifyContext = CreateContext();
        Assert.Null(result);
    }
}