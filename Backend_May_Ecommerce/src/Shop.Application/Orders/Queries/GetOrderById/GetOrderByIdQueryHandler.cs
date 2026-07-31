using MediatR;
using Shop.Domain.Repositories;

namespace Shop.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetOrderWithItemAsync(request.OrderId, cancellationToken);
        if (order is null)
            return null;

        return new OrderDto(
            order.Id,
            order.UserId,
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAt,
            order.UpdatedAt,
            order.OrderItems.Select(item => new OrderItemDto(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.CalculateSubtotal()
            )).ToList()
        );
    }
}
