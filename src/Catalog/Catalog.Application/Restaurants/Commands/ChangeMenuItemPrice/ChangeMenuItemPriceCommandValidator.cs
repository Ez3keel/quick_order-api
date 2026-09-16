using FluentValidation;

namespace Catalog.Application.Restaurants.Commands.ChangeMenuItemPrice;

public sealed class ChangeMenuItemPriceCommandValidator : AbstractValidator<ChangeMenuItemPriceCommand>
{
    public ChangeMenuItemPriceCommandValidator()
    {
        RuleFor(c => c.RestaurantId).NotEmpty();
        RuleFor(c => c.MenuItemId).NotEmpty();
        RuleFor(c => c.NewPrice).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
    }
}
