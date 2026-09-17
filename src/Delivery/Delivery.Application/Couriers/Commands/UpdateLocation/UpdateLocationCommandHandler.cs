using Delivery.Application.Abstractions;
using Delivery.Application.Couriers.Exceptions;
using Delivery.Domain.Common;
using Delivery.Domain.Couriers;
using MediatR;

namespace Delivery.Application.Couriers.Commands.UpdateLocation;

public sealed class UpdateLocationCommandHandler(
    ICourierRepository repository,
    IUnitOfWork unitOfWork,
    ICourierAvailabilityIndex availabilityIndex) : IRequestHandler<UpdateLocationCommand>
{
    public async Task Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var courier = await repository.GetByIdAsync(CourierId.From(request.CourierId), cancellationToken)
            ?? throw new CourierNotFoundException(request.CourierId);

        courier.UpdateLocation(new GeoCoordinates(request.Latitude, request.Longitude));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Only Available couriers are assignment candidates — refreshing the index
        // while Busy would wrongly make them selectable again.
        if (courier.Status == CourierStatus.Available)
            await availabilityIndex.MarkAvailableAsync(request.CourierId, request.Latitude, request.Longitude, cancellationToken);
    }
}
