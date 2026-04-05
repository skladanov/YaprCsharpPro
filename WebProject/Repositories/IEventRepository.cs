using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEvents(Expression<Func<Event, bool>> predicate);
    Task<Event?> GetEvent(Guid id);
    Task<Event> AddEvent(EventDto newEventData);
    Task<bool> UpdateEvent(EventDto newEventData, Guid id);
    Task<bool> DeleteEvent(Guid id);
}