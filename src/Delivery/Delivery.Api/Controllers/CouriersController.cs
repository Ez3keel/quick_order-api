using Delivery.Api.Contracts;
using Delivery.Application.Couriers.Commands.GoOffline;
using Delivery.Application.Couriers.Commands.GoOnline;
using Delivery.Application.Couriers.Commands.RegisterCourier;
using Delivery.Application.Couriers.Commands.UpdateLocation;
using Delivery.Application.Couriers.Dtos;
using Delivery.Application.Couriers.Queries.GetCourierById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Delivery.Api.Controllers;

[ApiController]
[Route("api/couriers")]
public sealed class CouriersController(ISender sender) : ControllerBase
{
    [HttpPost]
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

    [HttpPost("{id:guid}/go-online")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GoOnline(Guid id, GoOnlineRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new GoOnlineCommand(id, request.Latitude, request.Longitude), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/location")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateLocation(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateLocationCommand(id, request.Latitude, request.Longitude), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/go-offline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GoOffline(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new GoOfflineCommand(id), cancellationToken);
        return NoContent();
    }
}
