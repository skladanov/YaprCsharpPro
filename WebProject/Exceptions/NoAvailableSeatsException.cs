public class NoAvailableSeatsException : BusinessException
{
    public NoAvailableSeatsException()
        : base("No available seats for this event") { }

    public NoAvailableSeatsException(Guid eventId)
        : base($"No available seats for this event with ID {eventId}") { }
}