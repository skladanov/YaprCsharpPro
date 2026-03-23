public interface IEventService
{
    PaginatedResult<Event> GetAllEvents(int page, int pageSize, string? title, DateTime? from, DateTime? to);
    Event? GetEvent(int id);
    Event AddEvent(EventDto newEventData);
    void UpdateEvent(EventDto newEventData, int id);
    void DeleteEvent(int id);
}