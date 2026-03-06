public class LocalEventRepository : IEventRepository
{
    private readonly ICollection<Event> _events = new List<Event>();

    public ICollection<Event> GetAllEvents()
    {
        return _events;
    }
    public Event GetEvent(int id)
    {
        var eventItem = _events.FirstOrDefault(e => e.Id == id);
        if (eventItem == null)
            throw new KeyNotFoundException($"Event with ID {id} not found.");

        return eventItem;
    }
    public void AddEvent(Event eventItem)
    {
        if(eventItem == null)
            throw new ArgumentNullException(nameof(eventItem));

        if(_events.Any(e => e.Id == eventItem.Id))
            throw new InvalidOperationException($"Event with ID {eventItem.Id} already exists.");

        _events.Add(eventItem);
    }

    public void DeleteEvent(int id)
    {
        var eventItem = GetEvent(id);
        _events.Remove(eventItem);
    }
}