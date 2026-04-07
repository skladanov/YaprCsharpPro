public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllEventsAsync(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null);
    Task<Event?> GetEventAsync(Guid id);
    Task<Event> AddEventAsync(EventDto newEventData);
    Task UpdateEventAsync(EventDto newEventData, Guid id);
    Task DeleteEventAsync(Guid id);
}