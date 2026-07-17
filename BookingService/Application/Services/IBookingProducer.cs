public interface IBookingProducer
{
    Task PublishAsync(object evt, CancellationToken token);
}