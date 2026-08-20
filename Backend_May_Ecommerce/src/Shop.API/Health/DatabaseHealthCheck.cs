using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shop.Application.Common.Diagnostics;

namespace Shop.API.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IHealthProbe _probe;

    public DatabaseHealthCheck(IHealthProbe probe) => _probe = probe;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var reachable = await _probe.CanReachDatabaseAsync(cancellationToken);

        return reachable
            ? HealthCheckResult.Healthy("PostgreSQL reachable.")
            : HealthCheckResult.Unhealthy("PostgreSQL unreachable.");
    }
}
