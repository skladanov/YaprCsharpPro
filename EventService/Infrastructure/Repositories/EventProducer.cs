using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class EventProducer : IEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer; // Храним готовый инстанс
    private bool _disposed;

    public EventProducer(IOptions<KafkaOptions> kafkaOptions)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServer,
            ClientId = kafkaOptions.Value.ClientId
            // Для продакшена добавьте SecurityProtocol, SaslMechanism и т.д.
        };

        // Создаем билдер из заполненного конфига
        var builder = new ProducerBuilder<string, string>(producerConfig);

        // Сохраняем единственный инстанс продюсера
        _producer = builder.Build();
    }

    public async Task PublishAsync(object evt, CancellationToken token)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventProducer));

        var topic = evt switch
        {
            BookingConfirmedEvent _ => EventTopic.Confirmed,
            BookingRejectedEvent _ => EventTopic.Rejected,
            _ => throw new ArgumentException("Unknown event type")
        };

        var json = System.Text.Json.JsonSerializer.Serialize(evt);

        // Используем уже созданный синглтон-продюсер
        var deliveryReport = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = evt.GetType().Name,
            Value = json
        }, token);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _producer?.Dispose(); // Корректно закрываем соединение с Kafka
    }
}