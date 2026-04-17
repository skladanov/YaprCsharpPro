using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token);
    Task<Event?> GetEventAsync(Guid existingEventId, CancellationToken token);
    Task<Guid> AddEventAsync(CreateEvent newEventData, CancellationToken token);
    Task UpdateEventAsync(CreateEvent newEventData, Guid existingEventId, CancellationToken token);
    Task DeleteEventAsync(Guid existingEventId, CancellationToken token);
}