using Delivery.Application.Abstractions;
using Delivery.Application.Assignments.Exceptions;
using Delivery.Application.Couriers.Exceptions;
using Delivery.Domain.Assignments;
using Delivery.Domain.Couriers;
using MediatR;

namespace Delivery.Application.Assignments.Commands.AssignCourier;

public sealed class AssignCourierCommandHandler(
    ICourierAvailabilityIndex availabilityIndex,
    ICourierRepository courierRepository,
    IDeliveryAssignmentRepository assignmentRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<AssignCourierCommand, Guid>
{
    public async Task<Guid> Handle(AssignCourierCommand request, CancellationToken cancellationToken)
    {
        var courierId = await availabilityIndex.PickAvailableCourierAsync(cancellationToken)
            ?? throw new NoCourierAvailableException(request.OrderId);

        var courier = await courierRepository.GetByIdAsync(CourierId.From(courierId), cancellationToken)
            ?? throw new CourierNotFoundException(courierId);

        courier.Reserve();

        var now = timeProvider.GetUtcNow();
        var assignment = DeliveryAssignment.Create(request.OrderId, courier.Id, now);
        assignmentRepository.Add(assignment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reflect the reservation in the live index only after the transactional
        // write succeeds — if SaveChanges fails, the courier stays selectable.
        await availabilityIndex.MarkUnavailableAsync(courierId, cancellationToken);

        return courier.Id.Value;
    }
}
