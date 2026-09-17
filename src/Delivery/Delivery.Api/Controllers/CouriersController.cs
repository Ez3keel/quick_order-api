using Delivery.Api.Contracts;
using Delivery.Application.Couriers.Commands.GoOffline;
using Delivery.Application.Couriers.Commands.GoOnline;
using Delivery.Application.Couriers.Commands.RegisterCourier;
using Delivery.Application.Couriers.Commands.UpdateLocation;
using Delivery.Application.Couriers.Dtos;
using Delivery.Application.Couriers.Queries.GetCourierById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Delivery.Api.Controllers;

[ApiController]
[Route("api/couriers")]
[Authorize]
public sealed class CouriersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType<CourierDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CourierDto>> Register(RegisterCourierRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterCourierCommand(request.Name), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourierDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourierByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    // Note: these endpoints don't verify the route id belongs to the caller — that
    // needs a link between the JWT's UserId and this service's CourierId that
    // doesn't exist yet (Courier is registered with its own generated id, not the
    // caller's UserId). Requiring the Courier role is the enforcement today; per-
    // courier ownership is a natural follow-up once that link exists.
    [HttpPost("{id:guid}/go-online")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GoOnline(Guid id, GoOnlineRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new GoOnlineCommand(id, request.Latitude, request.Longitude), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/location")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateLocation(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateLocationCommand(id, request.Latitude, request.Longitude), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/go-offline")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GoOffline(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new GoOfflineCommand(id), cancellationToken);
        return NoContent();
    }
}
