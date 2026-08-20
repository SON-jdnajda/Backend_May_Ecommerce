using Shop.Domain.Entities;

namespace Shop.Domain.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    /// <summary>
    /// Tracked batch load. One round trip for a whole order instead of N.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Untracked page of products plus the total row count. Paging happens in
    /// SQL (LIMIT/OFFSET), so the API never materialises the whole table.
    /// </summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);
}
