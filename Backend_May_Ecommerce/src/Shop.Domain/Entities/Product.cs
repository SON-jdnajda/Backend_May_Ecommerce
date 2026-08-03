using Shop.Domain.Common;
using Shop.Domain.Exceptions;

namespace Shop.Domain.Entities;

public sealed class Product : BaseEntity<Guid>, IAggregateRoot
{
    public string Name {get; private set;} = default!;
    public string? Description { get; private set;}
    public decimal Price {get; private set;}
    public int StockQuantity {get; private set;}
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = default!;

    public int Version {get; private set;}

    private Product() {}

    public Product(string name, string? description, decimal price, int stockQuantity, Guid categoryId)
    {
        if(string.IsNullOrWhiteSpace(name))
            throw new InvalidProductException("Tên sản phẩm không được để trống");
        if(name.Length > 200)
            throw new InvalidProductException("Tên sản phẩm không được vượt quá 200 ký tự");
        if(price < 0)
            throw new InvalidProductException("Giá sản phẩm không được âm");
        if(stockQuantity < 0)
            throw new InvalidProductException("Số lượng tồn kho không được âm");
        if(categoryId == Guid.Empty)
            throw new InvalidProductException("Sản phẩm phải thuộc một danh mục");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
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
        Version++;
        MarkUpdated();
    }

    public void IncreaseStock(int quantity)
    {
        if(quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng phải lớn hơn 0");

        StockQuantity += quantity;
        Version++;
        MarkUpdated();
    }
}
