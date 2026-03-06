public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository) {  _repository = repository; }

    public ICollection<Event> GetAllEvents()
    {
        return _repository.GetAllEvents();
    }
    public Event GetEvent(int id)
    {
        return _repository.GetEvent(id);
    }
    public void AddEvent(Event eventItem)
    {
        _repository.AddEvent(eventItem);
    }
    public void DeleteEvent(int id)
    {
        _repository.DeleteEvent(id);
    }
}