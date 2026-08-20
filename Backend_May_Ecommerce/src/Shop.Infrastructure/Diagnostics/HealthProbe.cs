using Microsoft.Extensions.Caching.Distributed;
using Shop.Application.Common.Diagnostics;
using Shop.Infrastructure.Persistence;

namespace Shop.Infrastructure.Diagnostics;

/// <summary>
/// Cheap reachability checks. Deliberately NOT a real query: a readiness probe
/// runs every few seconds and must not add load to a struggling dependency.
/// </summary>
public sealed class HealthProbe : IHealthProbe
{
    private const string CacheProbeKey = "health:probe";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _cache;

    public HealthProbe(ApplicationDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<bool> CanReachDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // A probe reports "not ready", it never propagates the failure -
            // an exception here would turn a degraded dependency into a 500.
            return false;
        }
    }

    public async Task<bool> CanReachCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            // A miss is still proof of a working round trip to Redis.
            await _cache.GetAsync(CacheProbeKey, cancellationToken);
            return true;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
