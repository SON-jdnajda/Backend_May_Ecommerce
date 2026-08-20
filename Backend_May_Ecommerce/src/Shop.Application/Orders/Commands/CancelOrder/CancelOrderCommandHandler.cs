using MediatR;
using Shop.Application.Common.Diagnostics;
using Shop.Domain.Enums;
using Shop.Domain.Repositories;

namespace Shop.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderMetrics _metrics;

    public CancelOrderCommandHandler(
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IOrderMetrics metrics)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetOrderWithItemsTrackedAsync(request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order with ID '{request.OrderId}' was not found");

        // Guard BEFORE restoring stock. Order.CancelOrder() is already idempotent,
        // but without this a repeated cancel would restock the same items twice.
        if (order.Status == OrderStatus.Cancelled)
            return true;

        order.CancelOrder();

        var productIds = order.OrderItems.Select(i => i.ProductId).Distinct().ToList();
        var products = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var item in order.OrderItems)
        {
            // A product deleted after the order was placed is skipped rather than
            // failing the cancellation - the customer should still get cancelled.
            if (products.TryGetValue(item.ProductId, out var product))
                product.IncreaseStock(item.Quantity);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The early return above means a repeated cancel is not counted twice.
        _metrics.OrderCancelled();

        return true;
    }
}
