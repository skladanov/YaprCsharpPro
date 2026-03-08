public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository) {  _repository = repository; }

    public ICollection<Event> GetAllEvents()
    {
        return _repository.GetAllEvents();
    }
    public Event? GetEvent(int id)
    {
        return _repository.GetEvent(id);
    }
    public bool AddEvent(Event eventItem)
    {
        return _repository.AddEvent(eventItem);
    }
    public bool UpdateEvent(Event eventItem, int id)
    {
        return _repository.UpdateEvent(eventItem, id);
    }
    public bool DeleteEvent(int id)
    {
        return _repository.DeleteEvent(id);
    }
}