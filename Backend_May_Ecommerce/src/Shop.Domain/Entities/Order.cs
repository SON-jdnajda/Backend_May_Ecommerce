using Shop.Domain.Common;
using Shop.Domain.Enums;
using Shop.Domain.Exceptions;

namespace Shop.Domain.Entities;

public class Order : BaseEntity<Guid>, IAggregateRoot
{
    private readonly List<OrderItem> _orderItems = new();

    public Guid UserId {get; private set;}
    public decimal TotalAmount {get; private set;}
    public OrderStatus Status {get; private set;}

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private Order () {}

    public Order(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new InvalidOrderException("UserId is required");

        Id = Guid.NewGuid();
        UserId = userId;
        Status = OrderStatus.Pending;
        TotalAmount = 0;
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if(Status != OrderStatus.Pending)
            throw new InvalidOrderException("Cannot add items to an order that is not Pending");

        if(_orderItems.Any(i => i.ProductId == productId))
            throw new InvalidOrderException("Product is already added to this order");

        _orderItems.Add(new OrderItem(productId, productName, unitPrice, quantity));
        RecalculateTotalAmount();
        MarkUpdated();
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = _orderItems.Sum(item => item.CalculateSubtotal());
    }

    public void CancelOrder()
    {
        if(Status == OrderStatus.Completed)
            throw new InvalidOrderException("Completed orders cannot be cancelled");

        // Cancelling an already-cancelled order is a no-op, not an error:
        // a client retry must not fail.
        if(Status == OrderStatus.Cancelled)
            return;

        Status = OrderStatus.Cancelled;
        MarkUpdated();
    }
}
