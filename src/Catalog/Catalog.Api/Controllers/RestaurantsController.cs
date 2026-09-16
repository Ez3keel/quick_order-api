using Catalog.Api.Contracts;
using Catalog.Application.Restaurants.Commands.AddMenuItem;
using Catalog.Application.Restaurants.Commands.ChangeMenuItemPrice;
using Catalog.Application.Restaurants.Commands.RegisterRestaurant;
using Catalog.Application.Restaurants.Dtos;
using Catalog.Application.Restaurants.Queries.GetRestaurantById;
using Catalog.Application.Restaurants.Queries.ListRestaurants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public sealed class RestaurantsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RestaurantDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RestaurantDto>> Register(
        RegisterRestaurantRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterRestaurantCommand(request.Name), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<RestaurantDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RestaurantDto>>> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListRestaurantsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RestaurantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRestaurantByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/menu-items")]
    [ProducesResponseType<MenuItemDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MenuItemDto>> AddMenuItem(
        Guid id, AddMenuItemRequest request, CancellationToken cancellationToken)
    {
        var command = new AddMenuItemCommand(id, request.ProductName, request.Price, request.Currency);
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    [HttpPut("{id:guid}/menu-items/{menuItemId:guid}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeMenuItemPrice(
        Guid id, Guid menuItemId, ChangeMenuItemPriceRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangeMenuItemPriceCommand(id, menuItemId, request.NewPrice, request.Currency);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
