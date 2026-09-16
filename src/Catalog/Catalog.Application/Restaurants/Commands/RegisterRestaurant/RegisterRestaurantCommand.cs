using Catalog.Application.Restaurants.Dtos;
using MediatR;

namespace Catalog.Application.Restaurants.Commands.RegisterRestaurant;

public sealed record RegisterRestaurantCommand(string Name) : IRequest<RestaurantDto>;
