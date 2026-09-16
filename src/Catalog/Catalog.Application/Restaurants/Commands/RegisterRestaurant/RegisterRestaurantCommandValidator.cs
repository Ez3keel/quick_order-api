using FluentValidation;

namespace Catalog.Application.Restaurants.Commands.RegisterRestaurant;

public sealed class RegisterRestaurantCommandValidator : AbstractValidator<RegisterRestaurantCommand>
{
    public RegisterRestaurantCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
    }
}
