public interface IEventService
{
    PaginatedResult<Event> GetAllEvents(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null);
    Event? GetEvent(Guid id);
    Event AddEvent(EventDto newEventData);
    void UpdateEvent(EventDto newEventData, Guid id);
    void DeleteEvent(Guid id);
}