using Catalog.Application.Abstractions;
using Catalog.Application.Restaurants.Dtos;
using Catalog.Domain.Restaurants;
using MediatR;

namespace Catalog.Application.Restaurants.Commands.RegisterRestaurant;

public sealed class RegisterRestaurantCommandHandler(
    IRestaurantRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterRestaurantCommand, RestaurantDto>
{
    public async Task<RestaurantDto> Handle(RegisterRestaurantCommand request, CancellationToken cancellationToken)
    {
        var restaurant = Restaurant.Register(request.Name);

        repository.Add(restaurant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return restaurant.ToDto();
    }
}
