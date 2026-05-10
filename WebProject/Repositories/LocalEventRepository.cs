using AutoMapper;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

public class LocalEventRepository : IEventRepository
{
    List<Event> _events = new();

    public async Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token)
    {
        return _events.AsQueryable().Where(predicate).ToList();
    }

    public async Task<Event?> GetEventAsync(Guid eventId, CancellationToken token)
    {
        var existingEvent = _events.Where(e => e.Id == eventId).FirstOrDefault();

        return existingEvent;
    }

    public async Task<Guid> AddEventAsync(Event eventItem, CancellationToken token)
    {
        _events.Add(eventItem);
        return eventItem.Id;
    }

    public async Task<bool> UpdateEventAsync(Event eventItem, CancellationToken token)
    {
        var existingEvent = _events.Where(e => e.Id == eventItem.Id).FirstOrDefault();

        if (existingEvent == null) return false;

        existingEvent = eventItem;

        return true;
    }

    public async Task<bool> DeleteEventAsync(Guid existingEventId, CancellationToken token)
    {
        var existingEvent = _events.Where(e => e.Id == existingEventId).FirstOrDefault();

        if (existingEvent == null) return false;

        _events.Remove(existingEvent!);

        return true;
    }
}