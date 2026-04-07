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
                var pendingBookings = await _bookingRepository.GetPendingBookings();
                if (pendingBookings != null)
                    pendingBookings.ForEach(b => b.Status = Booking.BookingStatus.Confirmed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {

            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

    }
}