public class BookingForPastEventException : BusinessException
{
    public BookingForPastEventException()
        : base("Cannot create a booking for past event") { }

    public BookingForPastEventException(Guid eventId)
        : base($"Cannot create a booking for past event with ID {eventId}") { }

    public BookingForPastEventException(Guid bookingId, Guid eventId)
        : base($"Cannot create a booking with ID {bookingId} for past event with ID {eventId}") { }
}