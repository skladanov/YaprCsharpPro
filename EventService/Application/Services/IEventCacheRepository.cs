using System.Linq.Expressions;

public interface IEventCacheRepository
{
    Task<ICollection<ReturnedEvent>> GetTop10PopularEventsAsync();
    Task SetTop10PopularEventsAsync(ICollection<ReturnedEvent> events);

    Task<ReturnedEvent?> GetEventByIdAsync(Guid id);
    Task SetEventByIdAsync(Guid id, ReturnedEvent? eventDto);
    Task InvalidateEventByIdAsync(Guid id);
}
