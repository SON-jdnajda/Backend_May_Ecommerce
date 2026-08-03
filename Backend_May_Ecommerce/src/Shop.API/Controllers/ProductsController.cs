using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common;
using Shop.Application.Products.Commands.CreateProduct;
using Shop.Application.Products.Queries;
using Shop.Application.Products.Queries.GetProductById;
using Shop.Application.Products.Queries.GetProductsList;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var productId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = productId }, new { id = productId });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetProductsListQuery(page, pageSize), cancellationToken);

        return Ok(result);
    }


    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }
}
