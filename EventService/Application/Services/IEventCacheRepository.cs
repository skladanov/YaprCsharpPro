using System.Linq.Expressions;

public interface IEventCacheRepository
{
    Task<ICollection<Event>> GetTop10PopularEventsAsync();
    Task SetTop10PopularEventsAsync(ICollection<Event> events, TimeSpan ttl);

    Task<Event?> GetEventByIdAsync(Guid id);
    Task SetEventByIdAsync(Guid id, Event? eventDto, TimeSpan? ttl);
    Task InvalidateEventByIdAsync(Guid id);
}
