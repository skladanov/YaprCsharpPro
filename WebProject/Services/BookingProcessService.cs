public class BookingProcessService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingProcessService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                var pendingBookings = await _bookingRepository.GetPendingBookings();
                if (pendingBookings?.Any() == true)
                {
                    pendingBookings.ForEach(booking =>
                    {
                        booking.Status = Booking.BookingStatus.Confirmed;
                        booking.ProcessedAt = DateTime.Now;
                    });
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {

            }
        }
    }
}