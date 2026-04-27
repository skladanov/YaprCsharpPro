using System.Linq.Expressions;

public class BookingProcessService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BookingProcessService> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingProcessService(IServiceScopeFactory serviceScopeFactory, ILogger<BookingProcessService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking process service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingBookings = await GetPendingsAsync(stoppingToken);

            if (pendingBookings?.Any() == true)
            {
                _logger.LogInformation($"Processing {pendingBookings.Count()} pending bookings.");

                var tasks = pendingBookings.Select(booking => BookingProcessAsync(booking, stoppingToken));

                await Task.WhenAll(tasks);
            }
        }
    }

    private async Task BookingProcessAsync(Booking booking, CancellationToken stoppingToken)
    {
        await _processingSemaphore.WaitAsync();

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var _eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var _bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            Event? eventItem = await _eventRepository.GetEventAsync(booking.EventId, stoppingToken);

            if (eventItem == null)
                throw new EventNotFoundException(booking.EventId);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                booking.Confirm();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                booking.Reject();
                eventItem.ReleaseSeats();
                _logger.LogInformation("Booking process service is stopping due to cancellation request.");
                return;
            }
            catch (Exception ex)
            {
                booking.Reject();
                eventItem.ReleaseSeats();
                _logger.LogError(ex, $"An error occurred while processing bookings: {ex.Message}");
                return;
            }
            finally
            {
                await _bookingRepository.UpdateBookingAsync(booking, stoppingToken);
                _processingSemaphore.Release();
            }
        }

        _logger.LogInformation($"Successfully processed bookings with ID: {booking.Id}.");
    }

    private async Task<List<Booking>?> GetPendingsAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            Expression<Func<Booking, bool>> predicate = e =>
            (e.Status == Booking.BookingStatus.Pending);

            var result = await bookingRepository.GetBookingsAsync(predicate, stoppingToken);

            return result;
        }
    }

}