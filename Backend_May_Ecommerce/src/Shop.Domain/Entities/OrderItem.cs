using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class OrderItem : BaseEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    public OrderItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("ProductName is required", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("UnitPrice cannot be negative", nameof(unitPrice));

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        // OrderId is left unset on purpose: EF populates it from the
        // Order -> OrderItems relationship when the aggregate is saved.
    }

    public decimal CalculateSubtotal()
    {
        return UnitPrice * Quantity;
    }
}
