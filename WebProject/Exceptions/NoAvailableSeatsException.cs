public class NoAvailableSeatsException : BusinessException
{
    public NoAvailableSeatsException()
        : base("Event has no available seats") { }

    public NoAvailableSeatsException(Guid eventId)
        : base($"Event with ID {eventId} has no available seats") { }
}