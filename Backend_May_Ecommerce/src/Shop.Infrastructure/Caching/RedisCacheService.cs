using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Caching;

namespace Shop.Infrastructure.Caching;

internal sealed class RedisCacheService : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan OpenDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromMilliseconds(200);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    private long _lastFailureTicks;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await GetRawAsync(key, cancellationToken);
        if (payload is null) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Cache payload not deserializable for {Key}", key);
            return default;
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        return SetRawAsync(key, payload, expiration ?? DefaultTtl, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => RemoveRawAsync(key, cancellationToken);

    public async Task<string> GetVersionAsync(string scope, CancellationToken cancellationToken = default)
    {
        var versionKey = VersionKey(scope);
        var current = await GetRawAsync(versionKey, cancellationToken);
        if (current is not null) return current;
        var seed = NewStamp();
        await SetRawAsync(versionKey, seed, expiration: null, cancellationToken);
        return seed;
    }

    public Task BumpVersionAsync(string scope, CancellationToken cancellationToken = default)
        => SetRawAsync(VersionKey(scope), NewStamp(), expiration: null, cancellationToken);

    private async Task<string?> GetRawAsync(string key, CancellationToken cancellationToken)
    {
        if (IsOpen()) return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OperationTimeout);

            var payload = await _cache.GetStringAsync(key, cts.Token);
            RecordSuccess();
            return payload;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            RecordFailure();
            _logger.LogWarning(ex, "Cache GET failed for {Key}", key);
            return null;
        }
    }

    private async Task SetRawAsync(string key, string value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        if (IsOpen()) return;

        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration is not null)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OperationTimeout);

            await _cache.SetStringAsync(key, value, options, cts.Token);
            RecordSuccess();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            RecordFailure();
            _logger.LogWarning(ex, "Cache SET failed for {Key}", key);
        }
    }

    private async Task RemoveRawAsync(string key, CancellationToken cancellationToken)
    {
        if (IsOpen()) return;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OperationTimeout);

            await _cache.RemoveAsync(key, cts.Token);
            RecordSuccess();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            RecordFailure();
            _logger.LogWarning(ex, "Cache REMOVE failed for {Key}", key);
        }
    }

    private bool IsOpen()
    {
        var ticks = Volatile.Read(ref _lastFailureTicks);
        if (ticks == 0)
            return false;

        var openUntil = ticks + OpenDuration.Ticks;
        if (DateTime.UtcNow.Ticks < openUntil)
            return true;
        Volatile.Write(ref _lastFailureTicks, 0);
        return false;
    }

    private void RecordFailure() => Volatile.Write(ref _lastFailureTicks, DateTime.UtcNow.Ticks);

    private void RecordSuccess()
    {
        if (Volatile.Read(ref _lastFailureTicks) != 0)
        {
            Volatile.Write(ref _lastFailureTicks, 0);
        }
    }

    private static string VersionKey(string scope) => $"ver:{scope}";

    private static string NewStamp() => DateTime.UtcNow.Ticks.ToString();
}
