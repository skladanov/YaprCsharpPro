using AutoMapper;
using System.Linq.Expressions;

public class LocalEventRepository : IEventRepository
{
    private readonly IMapper _mapper;
    List<Event> _events = new();
    private int _nextId = 1;

    public LocalEventRepository(IMapper mapper)
    {
        _mapper = mapper;
    }

    public ICollection<Event> GetAllEvents(Expression<Func<Event, bool>> predicate)
    {
        return _events.AsQueryable().Where(predicate).ToList();
    }

    public Event? GetEvent(int id)
    {
        return _events.Where(e => e.Id == id).FirstOrDefault();
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

        _events.Add(newEventItem);
        return newEventItem;
    }

    public bool UpdateEvent(EventDto newEventData, int id)
    {
        var existingEvent = GetEvent(id);
        if (existingEvent == null)
            return false;

        _mapper.Map(newEventData, existingEvent);

        return true;
    }

    public bool DeleteEvent(int id)
    {
        var existingEvent = GetEvent(id);

        if (existingEvent == null) return false;

        _events.Remove(existingEvent);

        return true;
    }
}