using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Api.Contracts;
using Ordering.Application.Orders.Commands.CancelOrder;
using Ordering.Application.Orders.Commands.DispatchForDelivery;
using Ordering.Application.Orders.Commands.MarkDelivered;
using Ordering.Application.Orders.Commands.MarkReadyForAssignment;
using Ordering.Application.Orders.Commands.PlaceOrder;
using Ordering.Application.Orders.Commands.StartPreparing;
using Ordering.Application.Orders.Dtos;
using Ordering.Application.Orders.Queries.GetOrderById;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDto>> Place(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            request.CustomerId,
            request.RestaurantId,
            request.Items.Select(i => new PlaceOrderItem(i.MenuItemId, i.Quantity)).ToList());

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/start-preparing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartPreparing(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new StartPreparingCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-ready-for-assignment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkReadyForAssignment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkReadyForAssignmentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/dispatch-for-delivery")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DispatchForDelivery(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DispatchForDeliveryCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-delivered")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkDelivered(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkDeliveredCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
