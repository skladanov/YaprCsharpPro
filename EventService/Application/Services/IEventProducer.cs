public interface IEventProducer
{
    Task PublishAsync(object evt, CancellationToken token);
}