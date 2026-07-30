public class RedisCacheOptions
{
    // TTL для отдельного события: по умолчанию 1 час, но можно переопределить в appsettings
    public TimeSpan EventByIdTtl { get; set; } = TimeSpan.FromHours(1);

    // TTL для топ-10: по умолчанию 10 минут
    public TimeSpan Top10Ttl { get; set; } = TimeSpan.FromMinutes(10);
}