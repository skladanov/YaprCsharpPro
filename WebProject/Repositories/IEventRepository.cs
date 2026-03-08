public interface IEventRepository
{
    ICollection<Event> GetAllEvents();
    Event? GetEvent(int id);
    bool AddEvent(Event eventItem);
    bool UpdateEvent(Event eventItem, int id);
    bool DeleteEvent(int id);
}