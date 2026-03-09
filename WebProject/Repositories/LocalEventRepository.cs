using AutoMapper;
using Microsoft.AspNetCore.Mvc.Diagnostics;

public class LocalEventRepository : IEventRepository
{
    private readonly IMapper _mapper;
    Dictionary<int, Event> _events = new();
    private int _nextId = 1;

    public LocalEventRepository(IMapper mapper)
    {
        _mapper = mapper;
    }

    public ICollection<Event> GetAllEvents()
    {
        return _events.Values.ToList<Event>();
    }
    public Event? GetEvent(int id)
    {
        _events.TryGetValue(id, out var eventItem);
        return eventItem;
    }
    public Event AddEvent(EventDto eventDto)
    {
        Event newEventItem = new Event{
            Id = _nextId++,
            Title = eventDto.Title,
            Description = eventDto.Description,
            StartAt = eventDto.StartAt,
            EndAt = eventDto.EndAt
        };

        _events[newEventItem.Id] = newEventItem;
        return newEventItem;
    }

    public bool UpdateEvent(EventDto newEventData, int id)
    {
        if (!_events.ContainsKey(id))
            return false;

        Event existingEvent = _events[id];
        _mapper.Map(newEventData, existingEvent);

        return true;
    }

    public bool DeleteEvent(int id)
    {
        return _events.Remove(id);
    }
}