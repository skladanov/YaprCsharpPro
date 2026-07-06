public class ActiveBookingsLimitExceededException : BusinessException
{
    public ActiveBookingsLimitExceededException()
        : base("User has exceeded the limit of active bookings") { }

    public ActiveBookingsLimitExceededException(Guid userId)
        : base($"User {userId} has exceeded the limit of active bookings") { }

    public ActiveBookingsLimitExceededException(Guid bookingId, Guid userId)
        : base($"User {userId} has exceeded the limit of active bookings with ID {bookingId}") { }
}
