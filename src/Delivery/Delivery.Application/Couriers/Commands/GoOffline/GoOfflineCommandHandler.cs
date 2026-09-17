using Delivery.Application.Abstractions;
using Delivery.Application.Couriers.Exceptions;
using Delivery.Domain.Couriers;
using MediatR;

namespace Delivery.Application.Couriers.Commands.GoOffline;

public sealed class GoOfflineCommandHandler(
    ICourierRepository repository,
    IUnitOfWork unitOfWork,
    ICourierAvailabilityIndex availabilityIndex) : IRequestHandler<GoOfflineCommand>
{
    public async Task Handle(GoOfflineCommand request, CancellationToken cancellationToken)
    {
        var courier = await repository.GetByIdAsync(CourierId.From(request.CourierId), cancellationToken)
            ?? throw new CourierNotFoundException(request.CourierId);

        courier.GoOffline();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await availabilityIndex.MarkUnavailableAsync(request.CourierId, cancellationToken);
    }
}
