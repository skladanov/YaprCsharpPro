using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

public class BookingProcessService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BookingProcessService> _logger;

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
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var pendingBookings = await _bookingService.GetPendingsAsync(stoppingToken);


                if (pendingBookings?.Any() == true)
                {
                    _logger.LogInformation($"Processing {pendingBookings.Count()} pending bookings.");

                    var tasks = pendingBookings.Select(booking => _bookingService.BookingProcessAsync(booking, stoppingToken));

                    await Task.WhenAll(tasks);
                }
            }
            
            await Task.Delay(5000, stoppingToken);
        }
    }
}