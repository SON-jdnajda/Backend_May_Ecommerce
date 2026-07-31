using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Orders.Commands.CancelOrder;
using Shop.Application.Orders.Commands.CreateOrder;
using Shop.Application.Orders.Queries.GetOrderById;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = orderId }, new { id = orderId });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);

        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelOrderCommand(id), cancellationToken);

        return NoContent();
    }
}
