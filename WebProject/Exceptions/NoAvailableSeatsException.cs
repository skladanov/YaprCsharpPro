public class NoAvailableSeatsException : BusinessException
{
    public NoAvailableSeatsException()
        : base("Event not found") { }

    public NoAvailableSeatsException(Guid eventId)
        : base($"Event with ID {eventId} not found") { }
}