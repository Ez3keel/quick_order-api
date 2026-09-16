using Catalog.Domain.Restaurants;

namespace Catalog.Application.Abstractions;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(RestaurantId id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Restaurant>> ListAsync(CancellationToken cancellationToken);

    void Add(Restaurant restaurant);
}
