using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Infrastructure.Persistence;

namespace Shop.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    // AsNoTracking on both: these only ever feed read-side DTOs.
    public async Task<Order?> GetOrderWithItemAsync(Guid orderId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetOrderByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    public async Task<Order?> GetOrderWithItemsTrackedAsync(Guid orderId, CancellationToken cancellationToken)
        => await DbSet
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
}
