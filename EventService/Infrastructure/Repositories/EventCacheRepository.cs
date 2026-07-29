using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

public class EventCacheRepository : IEventCacheRepository
{
    private readonly IDatabase _redis;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<EventCacheRepository> _logger;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const string Top10Key = "events:top10";

    public EventCacheRepository(IConnectionMultiplexer redis, IOptions<RedisCacheOptions> cacheOptions, ILogger<EventCacheRepository> logger)
    {
        _logger = logger;
        _options = cacheOptions.Value;

        try
        {
            _redis = redis.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis полностью отключён. Все операции кэша будут возвращать cache miss.");
            _redis = null;
        }
    }

    public async Task<ICollection<ReturnedEvent>> GetTop10PopularEventsAsync()
    {
        if (_redis == null) return new List<ReturnedEvent>(); // cache miss
        try
        {
            var raw = await _redis.StringGetAsync(Top10Key);
            if (!raw.HasValue) return new List<ReturnedEvent>();

            var json = raw.ToString();
            return JsonSerializer.Deserialize<List<ReturnedEvent>>(json, _opts) ?? new List<ReturnedEvent>();
        }
        catch (RedisException ex) 
        {
            _logger.LogWarning(ex, "Redis недоступен при чтении Top10. Используем БД.");
            return new List<ReturnedEvent>(); // cache miss
        }
    }

    public async Task SetTop10PopularEventsAsync(ICollection<ReturnedEvent> events)
    {
        if (_redis == null) return;

        try
        {
            var json = JsonSerializer.Serialize(events, _opts);
            await _redis.StringSetAsync(Top10Key, json, _options.Top10Ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Не удалось записать Top10 в Redis. Данные будут пересчитаны из БД.");
        }
    }

    public async Task<ReturnedEvent?> GetEventByIdAsync(Guid id)
    {
        if (_redis == null) return null; // cache miss

        try
        {
            var key = $"events:{id}";
            var raw = await _redis.StringGetAsync(key);
            if (!raw.HasValue) return null;

            var json = raw.ToString();
            return JsonSerializer.Deserialize<ReturnedEvent>(json, _opts);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis недоступен при чтении события {EventId}. Используем БД.", id);
            return null; // cache miss
        }
    }

    public async Task SetEventByIdAsync(Guid id, ReturnedEvent? eventDto)
    {
        if (_redis == null) return;

        try
        {
            var key = $"events:{id}";
            if (eventDto == null)
            {
                await _redis.KeyDeleteAsync(key);
                return;
            }

            var json = JsonSerializer.Serialize(eventDto, _opts);
            await _redis.StringSetAsync(key, json, _options.EventByIdTtl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Не удалось записать событие {EventId} в Redis.", id);
        }
    }

    public async Task InvalidateEventByIdAsync(Guid id)
    {
        if (_redis == null) return;

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
