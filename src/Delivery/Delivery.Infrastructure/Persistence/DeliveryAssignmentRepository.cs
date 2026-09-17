using Delivery.Application.Abstractions;
using Delivery.Domain.Assignments;
using Microsoft.EntityFrameworkCore;

namespace Delivery.Infrastructure.Persistence;

public sealed class DeliveryAssignmentRepository(DeliveryDbContext dbContext) : IDeliveryAssignmentRepository
{
    public Task<DeliveryAssignment?> GetByIdAsync(DeliveryAssignmentId id, CancellationToken cancellationToken) =>
        dbContext.Assignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public void Add(DeliveryAssignment assignment) => dbContext.Assignments.Add(assignment);
}
