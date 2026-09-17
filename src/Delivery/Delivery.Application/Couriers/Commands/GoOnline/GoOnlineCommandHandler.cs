using Delivery.Application.Abstractions;
using Delivery.Application.Couriers.Exceptions;
using Delivery.Domain.Common;
using Delivery.Domain.Couriers;
using MediatR;

namespace Delivery.Application.Couriers.Commands.GoOnline;

public sealed class GoOnlineCommandHandler(
    ICourierRepository repository,
    IUnitOfWork unitOfWork,
    ICourierAvailabilityIndex availabilityIndex) : IRequestHandler<GoOnlineCommand>
{
    public async Task Handle(GoOnlineCommand request, CancellationToken cancellationToken)
    {
        var courier = await repository.GetByIdAsync(CourierId.From(request.CourierId), cancellationToken)
            ?? throw new CourierNotFoundException(request.CourierId);

        courier.GoOnline(new GeoCoordinates(request.Latitude, request.Longitude));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await availabilityIndex.MarkAvailableAsync(request.CourierId, request.Latitude, request.Longitude, cancellationToken);
    }
}
