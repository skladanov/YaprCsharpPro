public class EventNotFoundException : BusinessException
{
    public EventNotFoundException(int eventId)
        : base($"Enent with ID {eventId} not found") { }
}