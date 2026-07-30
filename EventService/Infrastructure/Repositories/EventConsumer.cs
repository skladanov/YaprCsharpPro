using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class EventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEventProducer _producer;
    private readonly ILogger<EventConsumer> _logger;
    private readonly KafkaOptions _kafkaOptions;
    private IConsumer<Ignore, string>? _consumer;

    public EventConsumer(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<KafkaOptions> kafkaOptions,
        IEventProducer producer,
        ILogger<EventConsumer> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaOptions = kafkaOptions.Value;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_kafkaOptions.BootstrapServer))
            throw new InvalidOperationException("KafkaOptions.BootstrapServer is required");

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
        var topics = new[] { BookingTopic.Created, BookingTopic.Cancelled };
        _consumer.Subscribe(topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = await Task.Run(() => _consumer.Consume(stoppingToken), stoppingToken);

                    if (consumeResult?.Message == null) continue;

                    switch (consumeResult.Topic)
                    {
                        case var topic when topic == BookingTopic.Created:
                            var created = JsonSerializer
                                .Deserialize<BookingCreatedEvent>(
                                consumeResult.Message.Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                            if (created == null) continue;
                            await HandleCreatedAsync(created, stoppingToken);
                            _consumer.Commit(consumeResult);
                            break;

                        case var topic when topic == BookingTopic.Cancelled:
                            var canceled = JsonSerializer
                                .Deserialize<BookingCanceledEvent>(
                                consumeResult.Message.Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                            if (canceled == null) continue;
                            await HandleCanceledAsync(canceled, stoppingToken);
                            _consumer.Commit(consumeResult);
                            break;

                        default: continue;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consumer error: {Reason}", ex.Error.Reason);
                }
            }
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    private async Task HandleCreatedAsync(BookingCreatedEvent created, CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<IEventCacheRepository>();

        _logger.LogInformation("BookingCreatedEvent!");

        var @event = await repo.GetEventAsync(created.EventId, ct);

        if (@event == null)
        {
            await _producer.PublishAsync(new BookingRejectedEvent(created.BookingId), ct);
            return;
        }

        if (!@event.TryReserveSeats(created.SeatsCount))
        {
            await _producer.PublishAsync(new BookingRejectedEvent(created.BookingId), ct);
            return;
        }

        await repo.UpdateEventAsync(@event, ct);

        await cache.InvalidateEventByIdAsync(@event.Id);

        await _producer.PublishAsync(new BookingConfirmedEvent(created.BookingId), ct);
    }


    private async Task HandleCanceledAsync(BookingCanceledEvent evt, CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<IEventCacheRepository>();

        _logger.LogInformation("BookingCanceledEvent!");

        var canceledEvent = await repo.GetEventAsync(evt.EventId, ct);

        if (canceledEvent == null) return;

        canceledEvent.ReleaseSeats(evt.SeatsCount);

        await repo.UpdateEventAsync(canceledEvent, ct);

        await cache.InvalidateEventByIdAsync(canceledEvent.Id);
    }
}