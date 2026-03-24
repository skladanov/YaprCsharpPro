using System.Linq.Expressions;

public interface IEventRepository
{
    ICollection<Event> GetAllEvents(string? title, DateTime? from, DateTime? to);
    Event? GetEvent(int id);
    Event AddEvent(EventDto newEventData);
    bool UpdateEvent(EventDto newEventData, int id);
    bool DeleteEvent(int id);
}