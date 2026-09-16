using Catalog.Application.Abstractions;
using Catalog.Application.Restaurants.Dtos;
using MediatR;

namespace Catalog.Application.Restaurants.Queries.ListRestaurants;

public sealed class ListRestaurantsQueryHandler(IRestaurantRepository repository)
    : IRequestHandler<ListRestaurantsQuery, IReadOnlyCollection<RestaurantDto>>
{
    public async Task<IReadOnlyCollection<RestaurantDto>> Handle(
        ListRestaurantsQuery request, CancellationToken cancellationToken)
    {
        var restaurants = await repository.ListAsync(cancellationToken);
        return restaurants.Select(r => r.ToDto()).ToList();
    }
}
