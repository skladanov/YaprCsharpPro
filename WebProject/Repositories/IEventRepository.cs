using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token);
    Task<Event?> GetEventAsync(Guid eventId, CancellationToken token);
    Task AddEventAsync(Event eventData, CancellationToken token);
    Task UpdateEventAsync(Event eventData, CancellationToken token);
    Task DeleteEventAsync(Guid eventId, CancellationToken token);
}