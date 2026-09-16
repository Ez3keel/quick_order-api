using FluentValidation;

namespace Catalog.Application.Restaurants.Commands.AddMenuItem;

public sealed class AddMenuItemCommandValidator : AbstractValidator<AddMenuItemCommand>
{
    public AddMenuItemCommandValidator()
    {
        RuleFor(c => c.RestaurantId).NotEmpty();
        RuleFor(c => c.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
    }
}
