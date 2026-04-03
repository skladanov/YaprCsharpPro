public class EventNotFoundException : BusinessException
{
    public EventNotFoundException()
        : base("Event not found") { }

    public EventNotFoundException(int eventId)
        : base($"Event with ID {eventId} not found") { }
}