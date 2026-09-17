using FluentValidation;

namespace Delivery.Application.Couriers.Commands.RegisterCourier;

public sealed class RegisterCourierCommandValidator : AbstractValidator<RegisterCourierCommand>
{
    public RegisterCourierCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
    }
}
