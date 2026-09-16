using Catalog.Application.Restaurants.Dtos;
using MediatR;

namespace Catalog.Application.Restaurants.Queries.GetRestaurantById;

public sealed record GetRestaurantByIdQuery(Guid RestaurantId) : IRequest<RestaurantDto?>;
