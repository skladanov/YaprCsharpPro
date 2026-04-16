using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token);
    Task<Event?> GetEventAsync(Guid existingEventId, CancellationToken token);
    Task<Guid> AddEventAsync(EventDto newEventData, CancellationToken token);
    Task UpdateEventAsync(EventDto newEventData, Guid existingEventId, CancellationToken token);
    Task DeleteEventAsync(Guid existingEventId, CancellationToken token);
}