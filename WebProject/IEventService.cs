public interface IEventService
{
    List<Event> GetAllEvents();
    Event GetEvent(int Id);
    bool CreateEvent(Event evet);
    bool DeleteEvent(int Id);
}