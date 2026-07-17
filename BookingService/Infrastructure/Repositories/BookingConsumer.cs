using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

public class BookingConsumer : IHostedService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingConsumer> _logger;
    private CancellationTokenRegistration? _cancellation;
    private Task? _consumeTask;

    // зависимости только из Infrastructure и Shared.Contracts
    public BookingConsumer(
        IBookingRepository bookingRepository,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<BookingConsumer> logger)
    {
        _bookingRepository = bookingRepository;
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
        consumer.Subscribe("booking.confirmed");

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
        if (evt is BookingConfirmedEvent confirmed)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(confirmed.BookingId, ct);
            if (booking == null) return;
            booking.Confirm();
            await _bookingRepository.UpdateBookingAsync(booking, ct);
        }
        else if (evt is BookingRejectedEvent rejected)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(rejected.BookingId, ct);
            if (booking == null) return;
            booking.Reject();
            await _bookingRepository.UpdateBookingAsync(booking, ct);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _consumeTask ?? Task.CompletedTask;
}

