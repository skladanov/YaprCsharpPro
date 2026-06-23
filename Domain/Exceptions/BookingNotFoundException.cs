public class BookingNotFoundException : BusinessException
{
    public BookingNotFoundException()
        : base("Event has no available seats") { }

    public BookingNotFoundException(Guid bookingId)
        : base($"Event with ID {bookingId} has no available seats") { }
}