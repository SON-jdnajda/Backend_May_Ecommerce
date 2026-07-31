using MediatR;

namespace Shop.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
) : IRequest<Guid>;
