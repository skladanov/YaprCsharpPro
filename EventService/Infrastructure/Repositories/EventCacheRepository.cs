using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

public class EventCacheRepository : IEventCacheRepository
{
    private readonly IDatabase _redis;
    private readonly ILogger<EventCacheRepository> _logger;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const string Top10Key = "events:top10";

    public EventCacheRepository(IConnectionMultiplexer redis, ILogger<EventCacheRepository> logger)
    {
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<ICollection<Event>> GetTop10PopularEventsAsync()
    {
        try
        {
            var raw = await _redis.StringGetAsync(Top10Key);
            if (!raw.HasValue) return new List<Event>();

            var json = raw.ToString();
            return JsonSerializer.Deserialize<List<Event>>(json, _opts) ?? new List<Event>();
        }
        catch (RedisException ex) 
        {
            _logger.LogWarning(ex, "Redis недоступен при чтении Top10. Используем БД.");
            return new List<Event>(); // cache miss
        }
    }

    public async Task SetTop10PopularEventsAsync(ICollection<Event> events, TimeSpan ttl)
    {
        try
        {
            var json = JsonSerializer.Serialize(events, _opts);
            await _redis.StringSetAsync(Top10Key, json, ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Не удалось записать Top10 в Redis. Данные будут пересчитаны из БД.");
        }
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        try
        {
            var key = $"events:{id}";
            var raw = await _redis.StringGetAsync(key);
            if (!raw.HasValue) return null;

            var json = raw.ToString();
            return JsonSerializer.Deserialize<Event>(json, _opts);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis недоступен при чтении события {EventId}. Используем БД.", id);
            return null; // cache miss
        }
    }

    public async Task SetEventByIdAsync(Guid id, Event? eventDto, TimeSpan? ttl)
    {
        try
        {
            var key = $"events:{id}";
            if (eventDto == null)
            {
                await _redis.KeyDeleteAsync(key);
                return;
            }

            var json = JsonSerializer.Serialize(eventDto, _opts);
            var expiry = ttl ?? TimeSpan.FromHours(1);
            await _redis.StringSetAsync(key, json, expiry);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Не удалось записать событие {EventId} в Redis.", id);
        }
    }

    public async Task InvalidateEventByIdAsync(Guid id)
    {
        try
        {
            var key = $"events:{id}";
            await _redis.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Не удалось удалить событие {EventId} из Redis.", id);
        }
    }
}
