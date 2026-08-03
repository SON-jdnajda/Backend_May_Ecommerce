using MediatR;
using Shop.Application.Common;

namespace Shop.Application.Products.Queries.GetProductsList;

public record GetProductsListQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
