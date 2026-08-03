using MediatR;
using Shop.Application.Common;
using Shop.Domain.Repositories;

namespace Shop.Application.Products.Queries.GetProductsList;

public class GetProductsListQueryHandler : IRequestHandler<GetProductsListQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsListQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        var allProducts = await _productRepository.GetAllAsync(cancellationToken);
        var totalCount = allProducts.Count;

        var items = allProducts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.CategoryId))
            .ToList();

       return new PagedResult<ProductDto>(items, totalCount, request.Page, request.PageSize);
    }
}
