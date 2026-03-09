public interface IEventService
{
    ICollection<Event> GetAllEvents();
    Event? GetEvent(int id);
    Event AddEvent(EventDto newEventData);
    bool UpdateEvent(EventDto newEventData, int id);
    bool DeleteEvent(int id);
}