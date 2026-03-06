public interface IEventService
{
    ICollection<Event> GetAllEvents();
    Event GetEvent(int Id);
    void AddEvent(Event evet);
    void DeleteEvent(int Id);
}