using System.Linq.Expressions;

public interface IEventRepository
{
    Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token);
    Task<Event?> GetEventAsync(Guid Id, CancellationToken token);
    Task AddEventAsync(Event @event, CancellationToken token);
    Task UpdateEventAsync(Event @event, CancellationToken token);
    Task DeleteEventAsync(Event @event, CancellationToken token);
}