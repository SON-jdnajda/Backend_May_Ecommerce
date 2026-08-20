using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Infrastructure.Persistence;

namespace Shop.Infrastructure.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    // Tracked on purpose: callers mutate StockQuantity on what this returns.
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

    // Untracked: the caller only projects to a DTO, so paying for the change
    // tracker on a whole page is wasted work.
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // OFFSET without ORDER BY is undefined in PostgreSQL - page 2 could
        // repeat rows from page 1.
        var query = DbSet.AsNoTracking().OrderBy(p => p.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
