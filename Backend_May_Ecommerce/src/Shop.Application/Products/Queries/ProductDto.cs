namespace Shop.Application.Products.Queries;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
);