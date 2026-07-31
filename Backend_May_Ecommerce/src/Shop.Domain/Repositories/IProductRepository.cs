using Shop.Domain.Entities;

namespace Shop.Domain.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    /// <summary>
    /// Tracked batch load. One round trip for a whole order instead of N.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
