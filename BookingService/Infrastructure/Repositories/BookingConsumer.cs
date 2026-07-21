using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using static Booking;

public class BookingConsumer : IHostedService
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_kafkaOptions.BootstrapServer))
            throw new InvalidOperationException("KafkaOptions.BootstrapServer is required");

        _cancellation = cancellationToken.Register(() => { });

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

        _consumeTask = Task.Run(() => ConsumeLoop(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private void ConsumeLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer?.Consume(ct);
                if (consumeResult?.Message == null) continue;

                var evt = DeserializeEvent(consumeResult.Message.Value);
                if (evt == null)
                {
                    _logger.LogWarning(
                        "Unknown or invalid event in topic '{Topic}' at offset {Offset}",
                        consumeResult.Topic, consumeResult.Offset);
                    _consumer?.Commit(consumeResult);
                    continue;
                }

                // Важно: асинхронный вызов внутри синхронного цикла
                HandleEventAsync(evt, ct).Wait(ct);

                _consumer?.Commit(consumeResult); // коммит только после успеха
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while consuming Kafka message");
                // НЕ делаем коммит — сообщение придёт снова (retry)
            }
        }
    }

    private object? DeserializeEvent(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            if (TryDeserialize<BookingConfirmedEvent>(json, out var confirmed)) return confirmed;
            if (TryDeserialize<BookingRejectedEvent>(json, out var rejected)) return rejected;
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Kafka message");
            return null;
        }
    }

    private bool TryDeserialize<T>(string json, out T? result) where T : class
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        result = JsonSerializer.Deserialize<T>(json, options);
        return result != null;
    }

    private async Task HandleEventAsync(object evt, CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        if (evt is BookingConfirmedEvent confirmed)
        {
            var booking = await repo.GetBookingByIdAsync(confirmed.BookingId, ct);
            if (booking == null) return;

            if (booking.Status == BookingStatus.Confirmed)
            {
                _logger.LogDebug("Booking {Id} already confirmed, skipping.", confirmed.BookingId);
                return;
            }

            booking.Confirm();
            await repo.UpdateBookingAsync(booking, ct);
        }
        else if (evt is BookingRejectedEvent rejected)
        {
            var booking = await repo.GetBookingByIdAsync(rejected.BookingId, ct);
            if (booking == null) return;

            if (booking.Status == BookingStatus.Rejected)
            {
                _logger.LogDebug("Booking {Id} already rejected, skipping.", rejected.BookingId);
                return;
            }

            booking.Reject();
            await repo.UpdateBookingAsync(booking, ct);
        }
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
