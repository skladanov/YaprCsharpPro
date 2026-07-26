using Confluent.Kafka;
using Microsoft.Extensions.Options;

public class BookingProducer : IBookingProducer, IDisposable
{
    private readonly IProducer<string, string> _producer; // Храним готовый продюсер
    private bool _disposed;

    public BookingProducer(IOptions<KafkaOptions> kafkaOptions)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServer,
            ClientId = kafkaOptions.Value.ClientId
            // Если используются SASL/SSL, добавьте их сюда же
        };

        // Создаем билдер из заполненного конфига
        var builder = new ProducerBuilder<string, string>(producerConfig);

        // Сохраняем готовый инстанс
        _producer = builder.Build();
    }

    public async Task PublishAsync(object evt, CancellationToken token)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BookingProducer));

        var topic = evt switch
        {
            BookingCreatedEvent _ => BookingTopic.Created,
            BookingCanceledEvent _ => BookingTopic.Cancelled,
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