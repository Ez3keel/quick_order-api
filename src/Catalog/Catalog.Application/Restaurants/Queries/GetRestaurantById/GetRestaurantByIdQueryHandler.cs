using Catalog.Application.Abstractions;
using Catalog.Application.Restaurants.Dtos;
using Catalog.Domain.Restaurants;
using MediatR;

namespace Catalog.Application.Restaurants.Queries.GetRestaurantById;

/// <summary>
/// Cache-aside: try Redis first (cheap, hit rate should be high — menus are read far
/// more than written), fall back to Postgres on a miss and repopulate the cache.
/// </summary>
public sealed class GetRestaurantByIdQueryHandler(
    IRestaurantRepository repository,
    IMenuCache menuCache) : IRequestHandler<GetRestaurantByIdQuery, RestaurantDto?>
{
    public async Task<RestaurantDto?> Handle(GetRestaurantByIdQuery request, CancellationToken cancellationToken)
    {
        var cached = await menuCache.GetAsync(request.RestaurantId, cancellationToken);
        if (cached is not null)
            return cached;

        var restaurant = await repository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant is null)
            return null;

        var dto = restaurant.ToDto();
        await menuCache.SetAsync(dto, cancellationToken);
        return dto;
    }
}
