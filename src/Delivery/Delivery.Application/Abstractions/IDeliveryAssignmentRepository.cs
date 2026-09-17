using Delivery.Domain.Assignments;

namespace Delivery.Application.Abstractions;

public interface IDeliveryAssignmentRepository
{
    Task<DeliveryAssignment?> GetByIdAsync(DeliveryAssignmentId id, CancellationToken cancellationToken);

    void Add(DeliveryAssignment assignment);
}
