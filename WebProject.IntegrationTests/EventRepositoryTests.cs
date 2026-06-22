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

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
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
        await ResetDatabaseAsync();

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
        await ResetDatabaseAsync();

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
        await ResetDatabaseAsync();

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

    [Fact]
    public async Task Migrate_ShouldCreateTablesEventsAndBookingsWithForeignKey()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        // Assert: проверяем структуру БД напрямую
        await using var connection = new NpgsqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();

        // Проверяем существование таблицы events
        var existsEventsTable = await connection.TableExistsAsync("events");
        Assert.True(existsEventsTable, "Таблица events не создана после миграций");

        // Проверяем существование таблицы bookings
        var existsBookingsTable = await connection.TableExistsAsync("bookings");
        Assert.True(existsBookingsTable, "Таблица bookings не создана после миграций");

        // Проверяем наличие столбца event_id в bookings
        var hasEventIdColumn = await connection.ColumnExistsAsync("bookings", "event_id");
        Assert.True(hasEventIdColumn, "Столбец event_id не создан в таблице bookings");

        // Проверяем, что event_id — внешний ключ на events.id
        var isForeignKeySetUp = await connection.IsForeignKeyAsync("bookings", "event_id", "events", "id");
        Assert.True(isForeignKeySetUp, "Внешний ключ event_id -> events.id не настроен");

        // Дополнительно: проверяем, что столбец не допускает NULL
        var allowsNull = await connection.ColumnAllowsNullAsync("bookings", "event_id");
        Assert.False(allowsNull, "Столбец event_id допускает NULL, хотя должен быть внешним ключом");
    }
}

public static class NpgsqlExtensions
{
    public static async Task<bool> TableExistsAsync(this NpgsqlConnection connection, string tableName)
    {
        var cmd = new NpgsqlCommand(
            $"SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = @tableName)",
            connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        return (bool)(await cmd.ExecuteScalarAsync());
    }

    public static async Task<bool> ColumnExistsAsync(this NpgsqlConnection connection, string tableName, string columnName)
    {
        var cmd = new NpgsqlCommand(
            $"SELECT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_name = @tableName AND column_name = @columnName)",
            connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        cmd.Parameters.AddWithValue("columnName", columnName);
        return (bool)(await cmd.ExecuteScalarAsync());
    }

    public static async Task<bool> IsForeignKeyAsync(
        this NpgsqlConnection connection,
        string referencingTable,
        string referencingColumn,
        string referencedTable,
        string referencedColumn)
    {
        var cmd = new NpgsqlCommand(
            @"SELECT COUNT(*) > 0
              FROM pg_constraint c
              JOIN pg_class t ON c.conrelid = t.oid
              JOIN pg_class r ON c.confrelid = r.oid
              JOIN pg_attribute a1 ON c.conkey[1] = a1.attnum AND a1.attrelid = c.conrelid
              JOIN pg_attribute a2 ON c.confkey[1] = a2.attnum AND a2.attrelid = c.confrelid
              WHERE t.relname = @referencingTable
                AND r.relname = @referencedTable
                AND a1.attname = @referencingColumn
                AND a2.attname = @referencedColumn
                AND c.contype = 'f'",
            connection);

        cmd.Parameters.AddWithValue("referencingTable", referencingTable);
        cmd.Parameters.AddWithValue("referencedTable", referencedTable);
        cmd.Parameters.AddWithValue("referencingColumn", referencingColumn);
        cmd.Parameters.AddWithValue("referencedColumn", referencedColumn);

        return (bool)(await cmd.ExecuteScalarAsync()); ;
    }

    public static async Task<bool> ColumnAllowsNullAsync(this NpgsqlConnection connection, string tableName, string columnName)
    {
        var cmd = new NpgsqlCommand(
            $"SELECT is_nullable FROM information_schema.columns " +
            $"WHERE table_name = @tableName AND column_name = @columnName",
            connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        cmd.Parameters.AddWithValue("columnName", columnName);

        var result = await cmd.ExecuteScalarAsync();
        return result is string nullable && nullable.ToLower() == "yes";
    }
}
