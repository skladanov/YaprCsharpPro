using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token);
    Task<Event?> GetEventAsync(Guid id, CancellationToken token);
    Task<Event> AddEventAsync(EventDto newEventData, CancellationToken token);
    Task<bool> UpdateEventAsync(EventDto newEventData, Guid id, CancellationToken token);
    Task<bool> DeleteEventAsync(Guid id, CancellationToken token);
}