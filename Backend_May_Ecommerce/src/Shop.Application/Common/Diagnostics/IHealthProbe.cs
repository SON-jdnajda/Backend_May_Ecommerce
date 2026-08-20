namespace Shop.Application.Common.Diagnostics;

/// <summary>
/// Outbound port letting the API ask "is the backing store reachable?" without
/// referencing EF Core or Redis. The API owns the HTTP contract of /health,
/// Infrastructure owns what a reachability check actually costs.
/// </summary>
    public interface IHealthProbe
{
    Task<bool> CanReachDatabaseAsync(CancellationToken cancellationToken);

    Task<bool> CanReachCacheAsync(CancellationToken cancellationToken);
}





