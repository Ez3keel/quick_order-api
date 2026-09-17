using Delivery.Application.Abstractions;
using Delivery.Domain.Couriers;
using Microsoft.EntityFrameworkCore;

namespace Delivery.Infrastructure.Persistence;

public sealed class CourierRepository(DeliveryDbContext dbContext) : ICourierRepository
{
    public Task<Courier?> GetByIdAsync(CourierId id, CancellationToken cancellationToken) =>
        dbContext.Couriers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Courier courier) => dbContext.Couriers.Add(courier);
}
