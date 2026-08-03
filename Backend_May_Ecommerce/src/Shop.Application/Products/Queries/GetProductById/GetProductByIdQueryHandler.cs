using MediatR;
using Shop.Domain.Repositories;

namespace Shop.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // Untracked: this only ever projects to a DTO.
        var product = await _productRepository.GetByIdAsNoTrackingAsync(request.Id, cancellationToken);
        if (product is null)
            return null;

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId
        );
    }
}
