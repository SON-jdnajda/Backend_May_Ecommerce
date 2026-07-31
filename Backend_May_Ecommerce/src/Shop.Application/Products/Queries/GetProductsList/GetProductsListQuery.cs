using MediatR;

namespace Shop.Application.Products.Queries.GetProductsList;

public record GetProductsListQuery : IRequest<IReadOnlyList<ProductDto>>;
