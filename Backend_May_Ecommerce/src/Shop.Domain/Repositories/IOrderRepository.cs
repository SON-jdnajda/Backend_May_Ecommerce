using Shop.Domain.Entities;

namespace Shop.Domain.Repositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetOrderWithItemAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderWithItemsTrackedAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetOrderByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
