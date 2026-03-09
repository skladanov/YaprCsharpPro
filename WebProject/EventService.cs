using AutoMapper;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository) 
    {
        _repository = repository;
    }

    public ICollection<Event> GetAllEvents()
    {
        return _repository.GetAllEvents();
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