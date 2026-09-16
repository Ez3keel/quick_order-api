using Catalog.Application.Restaurants.Dtos;
using MediatR;

namespace Catalog.Application.Restaurants.Queries.ListRestaurants;

public sealed record ListRestaurantsQuery : IRequest<IReadOnlyCollection<RestaurantDto>>;
