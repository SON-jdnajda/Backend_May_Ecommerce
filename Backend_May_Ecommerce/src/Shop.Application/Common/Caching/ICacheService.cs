namespace Shop.Application.Common.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<string> GetVersionAsync(string scope, CancellationToken cancellationToken = default);
    Task BumpVersionAsync(string scope, CancellationToken cancellationToken = default);
}
