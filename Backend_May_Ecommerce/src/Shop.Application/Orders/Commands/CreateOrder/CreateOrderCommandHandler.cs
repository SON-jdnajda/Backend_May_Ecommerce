using MediatR;
using Shop.Application.Common.Diagnostics;
using Shop.Domain.Entities;
using Shop.Domain.Exceptions;
using Shop.Domain.Repositories;

namespace Shop.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderMetrics _metrics;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IOrderMetrics metrics)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

        // One round trip for the whole order instead of one per line item.
        var products = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        var order = new Order(request.UserId);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new InvalidOrderException($"Product '{item.ProductId}' does not exist");

            // Throws InsufficientStockException if oversold, and bumps Version.
            product.DecreaseStock(item.Quantity);

            // Name and price come from the DATABASE, never from the request.
            order.AddItem(product.Id, product.Name, product.Price, item.Quantity);
        }

        await _orderRepository.AddAsync(order, cancellationToken);

        // One transaction covers the order insert AND every stock decrement.
        // A lost concurrency race surfaces as ConcurrencyConflictException,
        // which CustomExceptionHandler maps to 409 - no try/catch needed here.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Recorded only after the commit succeeds. Counting before SaveChanges
        // would inflate the metric with orders lost to a concurrency conflict.
        _metrics.OrderPlaced(order.OrderItems.Count, order.TotalAmount);

        return order.Id;
    }
}
