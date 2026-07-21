using Confluent.Kafka;
using Microsoft.Extensions.Options;

public class EventProducer : IEventProducer, IDisposable
{
    private readonly ProducerConfig _config;
    private readonly string _topicPrefix = "booking";
    private readonly ProducerBuilder<string, string> _producerBuilder;
    private IProducer<string, string>? _producer;
    private bool _disposed;

    public EventProducer(IOptions<KafkaOptions> kafkaOptions)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServer,
            ClientId = kafkaOptions.Value.ClientId,
            // Для продакшена: SecurityProtocol, SaslMechanism и т.д.
        };

        _producerBuilder = new ProducerBuilder<string, string>(config);
        _producer = _producerBuilder.Build();
    }

    public async Task PublishAsync(object evt, CancellationToken token)
    {
        if (_disposed || _producer == null)
            throw new ObjectDisposedException(nameof(EventProducer));
        var topic = evt switch
        {
            BookingConfirmedEvent _ => EventTopic.Confirmed,
            BookingRejectedEvent _ => EventTopic.Rejected,
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _producer?.Dispose();
    }
}
