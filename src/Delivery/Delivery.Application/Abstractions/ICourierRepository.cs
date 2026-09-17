using Delivery.Domain.Couriers;

namespace Delivery.Application.Abstractions;

public interface ICourierRepository
{
    Task<Courier?> GetByIdAsync(CourierId id, CancellationToken cancellationToken);

    void Add(Courier courier);
}
