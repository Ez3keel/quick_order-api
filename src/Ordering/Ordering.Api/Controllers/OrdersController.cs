using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
[Authorize]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDto>> Place(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            CurrentUserId,
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
        if (result is null)
            return NotFound();

        // Customers can only see their own orders. Restaurant staff/couriers/admins
        // aren't restricted to "their" order here — modeling "the courier assigned to
        // this specific order" or "the restaurant that owns it" needs a cross-service
        // lookup this endpoint doesn't have yet (same class of gap as the
        // Notification Hub's group subscriptions, see docs item 32).
        if (User.IsInRole("Customer") && result.CustomerId != CurrentUserId)
            return Forbid();

        return Ok(result);
    }

    [HttpPost("{id:guid}/start-preparing")]
    [Authorize(Roles = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartPreparing(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new StartPreparingCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-ready-for-assignment")]
    [Authorize(Roles = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkReadyForAssignment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkReadyForAssignmentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/dispatch-for-delivery")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DispatchForDelivery(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DispatchForDeliveryCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-delivered")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkDelivered(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkDeliveredCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelOrderCommand(id), cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
