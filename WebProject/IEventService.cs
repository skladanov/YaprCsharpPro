public interface IEventService
{
    PaginatedResult<Event> GetAllEvents(int page, int pageSize, string? title, DateTime? from, DateTime? to);
    Event? GetEvent(int id);
    Event AddEvent(EventDto newEventData);
    bool UpdateEvent(EventDto newEventData, int id);
    bool DeleteEvent(int id);
}