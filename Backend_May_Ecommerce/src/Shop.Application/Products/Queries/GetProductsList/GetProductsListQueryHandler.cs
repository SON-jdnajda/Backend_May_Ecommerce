using MediatR;
using Shop.Application.Common;
using Shop.Application.Common.Caching;
using Shop.Domain.Repositories;

namespace Shop.Application.Products.Queries.GetProductsList;

public class GetProductsListQueryHandler : IRequestHandler<GetProductsListQuery, PagedResult<ProductDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cache;

    public GetProductsListQueryHandler(IProductRepository productRepository, ICacheService cache)
    {
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        var version = await _cache.GetVersionAsync(CacheScopes.Products, cancellationToken);

        // Key carries the version stamp: one bump orphans every page at once,
        // so invalidation costs a single write instead of N deletes.
        var cacheKey = $"products:v{version}:p{request.Page}:s{request.PageSize}";

        var cached = await _cache.GetAsync<PagedResult<ProductDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        // Paging happens in SQL. Skip/Take in memory would still drag the whole
        // table across the wire first, which is what made p95 2.3s at 50 VUs.
        var (products, totalCount) = await _productRepository.GetPagedAsync(
            request.Page, request.PageSize, cancellationToken);

        var items = products
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.CategoryId))
            .ToList();

        var result = new PagedResult<ProductDto>(items, totalCount, request.Page, request.PageSize);

        await _cache.SetAsync(cacheKey, result, CacheTtl, cancellationToken);

        return result;
    }
}
