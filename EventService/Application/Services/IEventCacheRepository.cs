using System.Linq.Expressions;

public interface IEventCacheRepository
{
    Task<ICollection<ReturnedEvent>> GetTop10PopularEventsAsync();
    Task SetTop10PopularEventsAsync(ICollection<ReturnedEvent> events, TimeSpan ttl);

    Task<ReturnedEvent?> GetEventByIdAsync(Guid id);
    Task SetEventByIdAsync(Guid id, ReturnedEvent? eventDto, TimeSpan? ttl);
    Task InvalidateEventByIdAsync(Guid id);
}
