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

    public async Task<Event?> GetEventAsync(Guid existingEventId, CancellationToken token)
    {
        var existingEvent = _events.Where(e => e.Id == existingEventId).FirstOrDefault();

        return existingEvent;
    }

    public async Task<Guid> AddEventAsync(CreateEvent eventDto, CancellationToken token)
    {
        Event newEventItem = Event.Create(
            Guid.NewGuid(),
            eventDto.Title,
            eventDto.StartAt,
            eventDto.EndAt,
            eventDto.TotalSeats,
            eventDto.Description
        );

        _events.Add(newEventItem);

        return newEventItem.Id;
    }

    public async Task UpdateEventAsync(CreateEvent newEventData, Guid existingEventId, CancellationToken token)
    {
        var existingEvent = _events.Where(e => e.Id == existingEventId).FirstOrDefault();

        if (existingEvent != null)
            throw new EventNotFoundException(existingEventId);

        existingEvent!.Title = newEventData.Title;
        existingEvent!.Description = newEventData.Description;
        existingEvent!.StartAt = newEventData.StartAt;
        existingEvent!.EndAt = newEventData.EndAt;
    }

    public async Task DeleteEventAsync(Guid existingEventId, CancellationToken token)
    {
        var existingEvent = _events.Where(e => e.Id == existingEventId).FirstOrDefault();

        if (existingEvent != null)
            throw new EventNotFoundException(existingEventId);

        _events.Remove(existingEvent!);
    }
}