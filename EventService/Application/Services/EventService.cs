using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IEventCacheRepository _cache;
    private readonly ILogger<EventService> _logger;
    private readonly TimeSpan _top10Ttl = TimeSpan.FromMinutes(5);

    public EventService(IEventRepository repository, IEventCacheRepository cache, ILogger<EventService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ICollection<ReturnedEvent>> GetTop10PopularAsync(CancellationToken token)
    {
        // 1. Пробуем кэш
        var cached = await _cache.GetTop10PopularEventsAsync();
        if (cached.Count > 0) return cached;

        // 2. Если нет — идём в БД
        var result = new List<ReturnedEvent>();
        var events = await _repository.GetTop10BySalesPercentageAsync(token);
        if(events.Count == 0) return result;

        foreach(var @event in events) 
        {
            var dto = new ReturnedEvent
            {
                Id = @event.Id,
                Title = @event.Title,
                Description = @event.Description,
                TotalSeats = @event.TotalSeats,
                AvailableSeats = @event.AvailableSeats
            };

            result.Add(dto);
        }

        // 3. Пишем в кэш
        await _cache.SetTop10PopularEventsAsync(result, _top10Ttl);

        return result;
    }

    public async Task<PaginatedResult<Event>> GetAllEventsAsync(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null, CancellationToken token = default)
    {
        _logger.LogInformation($"Attempting to retrieve events with filters: page={page}, pageSize={pageSize}, title='{title}', from='{from}', to='{to}'");

        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        ICollection<Event> allEvents = await _repository.GetAllEventsAsync(predicate, token);

        if (allEvents == null)
        {
            allEvents = new List<Event>();
        }

        var totalCount = allEvents.Count;
        var offset = (page - 1) * pageSize;

        var items = allEvents
            .OrderBy(e => e.StartAt)
            .Skip(offset)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation($"Successfully retrieved {items.Count} events out of {totalCount} total events.");

        return new PaginatedResult<Event>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReturnedEvent> GetEventAsync(Guid id, CancellationToken token)
    {
        _logger.LogInformation("Attempting to retrieve event with ID: {EventId}", id);

        var cached = await _cache.GetEventByIdAsync(id);
        if (cached != null) return cached;

        var existingEvent = await _repository.GetEventAsync(id, token);

        if(existingEvent == null)
            throw new EventNotFoundException(id);

        var dto = new ReturnedEvent
        {
            Id = existingEvent.Id,
            Title = existingEvent.Title,
            Description = existingEvent.Description,
            TotalSeats = existingEvent.TotalSeats,
            AvailableSeats = existingEvent.AvailableSeats
        };

        await _cache.SetEventByIdAsync(id, dto, TimeSpan.FromMinutes(15));

        _logger.LogInformation("Successfully retrieved event with ID: {EventId}.", id);

        return dto;
    }

    public async Task<Guid> AddEventAsync(CreateEvent newEventData, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to add a new event with: Title='{newEventData.Title}', Description='{newEventData.Description}', StartAt='{newEventData.StartAt}', EndAt='{newEventData.EndAt}'");

        Event newEventItem = Event.Create(
            Guid.NewGuid(),
            newEventData.Title,
            newEventData.StartAt,
            newEventData.EndAt,
            newEventData.TotalSeats,
            newEventData.Description
        );

        await _repository.AddEventAsync(newEventItem, token);

        _logger.LogInformation("Successfully added new event with ID: {EventId}.", newEventItem.Id);

        return newEventItem.Id;
    }

    public async Task UpdateEventAsync(UpdateEvent newEventData, Guid id, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to update event with ID='{id}' and new data: Title='{newEventData.Title}', Description='{newEventData.Description}', StartAt='{newEventData.StartAt}', EndAt='{newEventData.EndAt}'");
        var @event = await _repository.GetEventAsync(id, token);
        if (@event == null)
            throw new EventNotFoundException(id);

        @event.Title = newEventData.Title;
        @event.Description = newEventData.Description;
        @event.StartAt = newEventData.StartAt;
        @event.EndAt = newEventData.EndAt;

        await _repository.UpdateEventAsync(@event, token);

        // Инвалидируем кэш события по ID
        await _cache.InvalidateEventByIdAsync(id);

        _logger.LogInformation("Successfully updated event with ID: {EventId}.", id);
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to delete event with ID='{id}'");
        var @event = await _repository.GetEventAsync(id, token);
        if (@event == null)
            throw new EventNotFoundException(id);

        await _repository.DeleteEventAsync(@event, token);

        // Инвалидируем кэш события по ID
        await _cache.InvalidateEventByIdAsync(id);

        _logger.LogInformation("Successfully remove event with ID: {EventId}.", id);
    }
}