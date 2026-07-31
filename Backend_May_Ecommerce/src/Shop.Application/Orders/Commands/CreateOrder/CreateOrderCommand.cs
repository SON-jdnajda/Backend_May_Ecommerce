using MediatR;

namespace Shop.Application.Orders.Commands.CreateOrder;

public record CreateOrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderCommand(Guid UserId, List<CreateOrderItemDto> Items) : IRequest<Guid>;
