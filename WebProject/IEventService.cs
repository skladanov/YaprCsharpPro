public interface IEventService
{
    ICollection<Event> GetAllEvents(string? title = null, DateTime? from = null, DateTime? to = null);
    Event? GetEvent(int id);
    Event AddEvent(EventDto newEventData);
    bool UpdateEvent(EventDto newEventData, int id);
    bool DeleteEvent(int id);
}