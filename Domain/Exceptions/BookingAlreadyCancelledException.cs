namespace Domain.Exceptions
{
    public class BookingAlreadyCancelledException : BusinessException
    {
        public BookingAlreadyCancelledException()
            : base("Booking was cancelled") { }

        public BookingAlreadyCancelledException(Guid bookingId)
            : base($"Booking with ID {bookingId} was cancelled") { }
    }
}
