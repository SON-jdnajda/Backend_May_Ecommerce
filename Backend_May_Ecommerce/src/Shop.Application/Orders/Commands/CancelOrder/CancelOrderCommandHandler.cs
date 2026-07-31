using MediatR;
using Shop.Domain.Enums;
using Shop.Domain.Repositories;
using System.Reflection.Metadata.Ecma335;

namespace Shop.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(IProductRepository productRepository,IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetOrderWithItemsTrackedAsync(request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order with ID '{request.OrderId}' was not found");
        if(order.Status == OrderStatus.Cancelled)
        {
            return true;
        }
        order.CancelOrder();

        foreach (var item in order.OrderItems)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if(product !=  null)
            {
                product.IncreaseStock(item.Quantity);
            }
        }
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        return true;
    }
}
