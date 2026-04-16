public class BookingNotFoundException : BusinessException
{
    public BookingNotFoundException()
        : base("Booking not found") { }

    public BookingNotFoundException(Guid bookingId)
        : base($"Booking with ID {bookingId} not found") { }
}