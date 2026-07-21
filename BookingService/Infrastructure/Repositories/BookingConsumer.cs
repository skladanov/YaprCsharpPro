using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using static Booking;

public class BookingConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BookingConsumer> _logger;
    private readonly KafkaOptions _kafkaOptions;

    private IConsumer<Ignore, string>? _consumer;
    private Task? _consumeTask;
    private CancellationTokenRegistration? _cancellation;

    public BookingConsumer(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<BookingConsumer> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_kafkaOptions.BootstrapServer))
            throw new InvalidOperationException("KafkaOptions.BootstrapServer is required");

        _cancellation = stoppingToken.Register(() => { });

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServer,
            GroupId = _kafkaOptions.GroupId,
            ClientId = _kafkaOptions.ClientId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = _kafkaOptions.SessionTimeoutMs,
            MaxPollIntervalMs = _kafkaOptions.MaxPollIntervalMs
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        // Подписываемся на топики из Shared.Contracts
        var topics = new List<string>
        {
            EventTopic.Confirmed,
            EventTopic.Rejected
        };

        _consumer.Subscribe(topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer?.Consume(stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    // ОПРЕДЕЛЯЕМ ЛОГИКУ ПО ТОПИКУ
                    switch (consumeResult.Topic)
                    {
                        case var topic when topic == EventTopic.Confirmed:
                            var corfirmed = JsonSerializer
                                .Deserialize<BookingConfirmedEvent>(
                                consumeResult.Message.Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                            if (corfirmed == null) continue;
                            await HandleConfirmedAsync(corfirmed, stoppingToken);
                            _consumer.Commit(consumeResult);
                            break;

                        case var topic when topic == EventTopic.Rejected:
                            var rejected = JsonSerializer
                                .Deserialize<BookingRejectedEvent>(
                                consumeResult.Message.Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                            if (rejected == null) continue;
                            await HandleRejectedAsync(rejected, stoppingToken);
                            _consumer.Commit(consumeResult);
                            break;

                        default: continue;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while consuming Kafka message");
                }
            }
        }
        finally 
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    private async Task HandleConfirmedAsync(BookingConfirmedEvent evt, CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var booking = await repo.GetBookingByIdAsync(evt.BookingId, ct);
        if (booking == null) return;

        if (booking.Status == BookingStatus.Confirmed)
        {
            _logger.LogDebug("Booking {Id} already confirmed, skipping.", evt.BookingId);
            return;
        }

        booking.Confirm();
        await repo.UpdateBookingAsync(booking, ct);
    }

    private async Task HandleRejectedAsync(BookingRejectedEvent evt, CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var booking = await repo.GetBookingByIdAsync(evt.BookingId, ct);
        if (booking == null) return;

        if (booking.Status == BookingStatus.Rejected)
        {
            _logger.LogDebug("Booking {Id} already rejected, skipping.", evt.BookingId);
            return;
        }

        booking.Reject();
        await repo.UpdateBookingAsync(booking, ct);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellation?.Dispose();

        if (_consumer != null)
        {
            _consumer.Close(); // graceful shutdown
            _consumer.Dispose();
            _consumer = null;
        }

        if (_consumeTask != null)
        {
            await _consumeTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
