namespace Shop.Application.Orders.Queries.GetOrderById;

public record OrderDto(
   Guid Id,
   Guid UserId,
   decimal TotalAmount,
   string Status,
   DateTime CreatedAt,
   DateTime? UpdatedAt,
   IReadOnlyList<OrderItemDto> Items
);

public record OrderItemDto(
   Guid Id,
   Guid ProductId,
   string ProductName,
   decimal UnitPrice,
   int Quantity,
   decimal Subtotal
);
