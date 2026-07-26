public class KafkaOptions
{
    public string BootstrapServer { get; set; } = "string.Empty";
    public string ClientId { get; set; } = string.Empty;
    public string GroupId { get; set; } = "bookings-consumer-group";
    public int SessionTimeoutMs { get; set; } = 10_000;
    public int MaxPollIntervalMs { get; set; } = 300_000;
}