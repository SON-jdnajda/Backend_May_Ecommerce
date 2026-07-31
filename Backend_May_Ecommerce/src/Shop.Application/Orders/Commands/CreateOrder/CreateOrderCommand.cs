using MediatR;

namespace Shop.Application.Orders.Commands.CreateOrder;

// Only ProductId + Quantity: name and price are read from the database
// server-side, so a client cannot dictate what it pays.
public record CreateOrderItemDto(Guid ProductId, int Quantity);

public record CreateOrderCommand(Guid UserId, List<CreateOrderItemDto> Items) : IRequest<Guid>;
