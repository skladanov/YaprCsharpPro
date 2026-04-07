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

    public async Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate)
    {
        return _events.AsQueryable().Where(predicate).ToList();
    }

    public async Task<Event?> GetEventAsync(Guid id)
    {
        return _events.Where(e => e.Id == id).FirstOrDefault();
    }

    public async Task<Event> AddEventAsync(EventDto eventDto)
    {
        Event newEventItem = new Event{
            Id = Guid.NewGuid(),
            Title = eventDto.Title,
            Description = eventDto.Description,
            StartAt = eventDto.StartAt,
            EndAt = eventDto.EndAt
        };

        _events.Add(newEventItem);
        return newEventItem;
    }

    public async Task<bool> UpdateEventAsync(EventDto newEventData, Guid id)
    {
        var existingEvent = GetEventAsync(id);
        if (existingEvent == null)
            return false;

        _mapper.Map(newEventData, existingEvent);

        return true;
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        var existingEvent = await GetEventAsync(id);

        if (existingEvent == null) return false;

        _events.Remove(existingEvent);

        return true;
    }
}