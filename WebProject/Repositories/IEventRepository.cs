using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token);
    Task<Event?> GetEventAsync(Guid eventId, CancellationToken token);
    Task<Guid> AddEventAsync(Event eventData, CancellationToken token);
    Task<bool> UpdateEventAsync(Event eventData, CancellationToken token);
    Task<bool> DeleteEventAsync(Guid eventId, CancellationToken token);
}