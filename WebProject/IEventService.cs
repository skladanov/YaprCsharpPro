public interface IEventService
{
    List<Event> GetAllEvents();
    Event GetEvent(int Id);
    void CreateEvent(Event evet);
    void DeleteEvent(int Id);
}