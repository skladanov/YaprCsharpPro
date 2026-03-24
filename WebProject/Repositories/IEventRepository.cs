using System.Linq.Expressions;

public interface IEventRepository
{
    ICollection<Event> GetAllEvents(Expression<Func<Event, bool>> predicate);
    Event? GetEvent(int id);
    Event AddEvent(EventDto newEventData);
    bool UpdateEvent(EventDto newEventData, int id);
    bool DeleteEvent(int id);
}