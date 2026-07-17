using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

public class EventConsumer : IHostedService
{
    private readonly IEventRepository _eventRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventConsumer> _logger;
    private CancellationTokenRegistration? _cancellation;
    private Task? _consumeTask;

    // зависимости только из Infrastructure и Shared.Contracts
    public EventConsumer(
        IEventRepository eventRepository,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<EventConsumer> logger)
    {
        _eventRepository = eventRepository;
        _configuration = configuration;
        _logger = logger;

    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellation = cancellationToken.Register(() => { });
        _consumeTask = Task.Run(() => ConsumeLoop(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ConsumeLoop(CancellationToken ct)
    {
        var config = new ConsumerConfig { /* BootstrapServers, GroupId и т.п. */ };
        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe("booking.created");
        consumer.Subscribe("booking.canceled");

        while (!ct.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(ct);
            if (consumeResult?.Message == null) continue;

            // десериализация из Shared.Contracts
            var evt = JsonSerializer.Deserialize<BookingConfirmedEvent>(
                consumeResult.Message.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt == null)
            {
                consumer.Commit(consumeResult);
                continue;
            }

            await HandleEventAsync(evt, ct);
            consumer.Commit(consumeResult); // коммит только после успешной обработки
        }
    }

    private async Task HandleEventAsync(object evt, CancellationToken ct)
    {
        if (evt is BookingCreatedEvent created)
        {
            var @event = await _eventRepository.GetEventAsync(created.EventId, ct);
            if (@event == null) return;
            @event.ReleaseSeats(created.SeatsCount);
            await _eventRepository.UpdateEventAsync(@event, ct);
        }
        else if (evt is BookingCanceledEvent canceled)
        {
            var @event = await _eventRepository.GetEventAsync(canceled.EventId, ct);
            if (@event == null) return;
            @event.TryReserveSeats(canceled.SeatsCount);
            await _eventRepository.UpdateEventAsync(@event, ct);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _consumeTask ?? Task.CompletedTask;
}

