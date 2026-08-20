using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shop.Application.Common.Diagnostics;

namespace Shop.API.Health;

public sealed class CacheHealthCheck : IHealthCheck
{
    private readonly IHealthProbe _probe;

    public CacheHealthCheck(IHealthProbe probe) => _probe = probe;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var reachable = await _probe.CanReachCacheAsync(cancellationToken);
        return reachable
            ? HealthCheckResult.Healthy("Redis reachable.")
            : HealthCheckResult.Degraded("Redis unreachable - serving without cache.");
    }
}
