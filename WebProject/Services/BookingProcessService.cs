public class BookingProcessService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessService> _logger;

    public BookingProcessService(IBookingRepository bookingRepository, ILogger<BookingProcessService> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking process service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                var pendingBookings = await _bookingRepository.GetPendingBookings(stoppingToken);
                if (pendingBookings?.Any() == true)
                {
                    _logger.LogInformation($"Processing {pendingBookings.Count()} pending bookings.");

                    pendingBookings.ForEach(booking =>
                    {
                        booking.Status = Booking.BookingStatus.Confirmed;
                        booking.ProcessedAt = DateTime.Now;
                    });

                    _logger.LogInformation($"Successfully processed {pendingBookings.Count()} bookings.");
                }
                else
                {
                    _logger.LogDebug("No pending bookings found. Checking again in 2 seconds.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Booking process service is stopping due to cancellation request.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing bookings: {ErrorMessage}", ex.Message);
            }
        }
    }
}