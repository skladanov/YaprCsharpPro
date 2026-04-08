public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllEventsAsync(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null, CancellationToken token = default);
    Task<Event?> GetEventAsync(Guid id, CancellationToken token);
    Task<Event> AddEventAsync(EventDto newEventData, CancellationToken token);
    Task UpdateEventAsync(EventDto newEventData, Guid id, CancellationToken token);
    Task DeleteEventAsync(Guid id, CancellationToken token);
}