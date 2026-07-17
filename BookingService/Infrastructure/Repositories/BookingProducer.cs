using Microsoft.Extensions.Configuration;
using Confluent.Kafka;

public class BookingProducer : IBookingProducer
{
    private readonly ProducerConfig _config;
    private readonly string _topicPrefix = "booking";

    public BookingProducer(IConfiguration configuration)
    {
        _config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            ClientId = "booking-service-producer",
            // Для локальной разработки можно отключить некоторые проверки безопасности
            // SecurityProtocol = SecurityProtocol.SaslSsl,
            // SaslMechanism = SaslMechanism.Plain,
            // SaslUsername = "...",
            // SaslPassword = "...",
        };
    }

    public async Task PublishAsync(object evt, CancellationToken token)
    {
        var topic = evt switch
        {
            BookingCreatedEvent _ => $"{_topicPrefix}.created",
            BookingCanceledEvent _ => $"{_topicPrefix}.canceled",
            _ => throw new ArgumentException("Unknown event type")
        };

        using var producer = new ProducerBuilder<string, string>(_config).Build();

        var json = System.Text.Json.JsonSerializer.Serialize(evt);
        var deliveryReport = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = evt.GetType().Name,
            Value = json
        }, token);
    }
}
