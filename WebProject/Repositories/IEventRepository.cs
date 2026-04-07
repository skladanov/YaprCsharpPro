using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate);
    Task<Event?> GetEventAsync(Guid id);
    Task<Event> AddEventAsync(EventDto newEventData);
    Task<bool> UpdateEventAsync(EventDto newEventData, Guid id);
    Task<bool> DeleteEventAsync(Guid id);
}