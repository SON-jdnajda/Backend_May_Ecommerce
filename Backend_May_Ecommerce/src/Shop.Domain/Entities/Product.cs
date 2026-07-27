using Shop.Domain.Common;
using Shop.Domain.Exceptions;

namespace Shop.Domain.Entities;

public sealed class Product : BaseEntity<Guid>, IAggregateRoot
{
    public string Name {get; private set;} = default!;
    public string? Description {get; private set;}
    public decimal Price {get; private set;}
    public int StockQuantity {get; private set;}
    public Guid CategoryId {get; private set;}
    public uint Version {get; set;}

    private Product() {}

    public Product(string name, string? description, decimal price, int stockQuantity, Guid categoryId){
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
    }

    public void DecreaseStock(int quantity)
    {
        if(quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng phải lớn hơn 0");

        if(StockQuantity < quantity)
            throw new InsufficientStockException(Id, StockQuantity, quantity);

        StockQuantity -= quantity;
        MarkUpdated();
    }

    public void IncreaseStock(int quantity)
    {
        if(quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng phải lớn hơn 0");

        StockQuantity += quantity;
        MarkUpdated();
    }
}
