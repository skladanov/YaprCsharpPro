public class LocalEventRepository : IEventRepository
{
    private readonly ICollection<Event> _events = new List<Event>();

    public ICollection<Event> GetAllEvents()
    {
        return _events;
    }
    public Event? GetEvent(int id)
    {
        return _events.FirstOrDefault(e => e.Id == id);
    }
    public bool AddEvent(Event eventItem)
    {

        if (_events.Any(e => e.Id == eventItem.Id))
            return false;

        _events.Add(eventItem);
        return true;
    }

    public bool UpdateEvent(Event newEvent, int id)
    {
        var oldEvent = GetEvent(id);
        if (oldEvent == null) return false;

        oldEvent.Title = newEvent.Title;
        oldEvent.Description = newEvent.Description;
        oldEvent.StartAt = newEvent.StartAt;
        oldEvent.EndAt = newEvent.EndAt;
        return true;
    }

    public bool DeleteEvent(int id)
    {
        var eventItem = GetEvent(id);
        if (eventItem == null) return false;
        _events.Remove(eventItem);
        return true;
    }
}