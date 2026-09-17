using Delivery.Application.Abstractions;
using Delivery.Application.Couriers.Dtos;
using Delivery.Domain.Couriers;
using MediatR;

namespace Delivery.Application.Couriers.Commands.RegisterCourier;

public sealed class RegisterCourierCommandHandler(
    ICourierRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterCourierCommand, CourierDto>
{
    public async Task<CourierDto> Handle(RegisterCourierCommand request, CancellationToken cancellationToken)
    {
        var courier = Courier.Register(request.Name);

        repository.Add(courier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return courier.ToDto();
    }
}
