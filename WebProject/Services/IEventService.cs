public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllEventsAsync(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null, CancellationToken token = default);
    Task<Event> GetEventAsync(Guid id, CancellationToken token);
    Task<Guid> AddEventAsync(CreateEvent newEventData, CancellationToken token);
    Task UpdateEventAsync(UpdateEvent newEventData, Guid id, CancellationToken token);
    Task DeleteEventAsync(Guid id, CancellationToken token);
}