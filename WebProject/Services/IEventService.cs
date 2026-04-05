public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllEvents(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null);
    Task<Event?> GetEvent(Guid id);
    Task<Event> AddEvent(EventDto newEventData);
    Task UpdateEvent(EventDto newEventData, Guid id);
    Task DeleteEvent(Guid id);
}