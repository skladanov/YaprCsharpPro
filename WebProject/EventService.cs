using AutoMapper;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository) 
    {
        _repository = repository;
    }
    public ICollection<Event> GetAllEvents(string? title = "" , DateTime? from = null, DateTime? to = null)
    {
        var query = _repository.GetAllEvents().AsQueryable();

        if (!string.IsNullOrEmpty(title))
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);

        return query.ToList();
    }
    public Event? GetEvent(int id)
    {
        return _repository.GetEvent(id);
    }
    public Event AddEvent(EventDto newEventData)
    {
        return _repository.AddEvent(newEventData);
    }
    public bool UpdateEvent(EventDto newEventData, int id)
    {
        return _repository.UpdateEvent(newEventData, id);
    }
    public bool DeleteEvent(int id)
    {
        return _repository.DeleteEvent(id);
    }
}