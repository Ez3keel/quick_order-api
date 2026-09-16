using Catalog.Application.Abstractions;
using Catalog.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class RestaurantRepository(CatalogDbContext dbContext) : IRestaurantRepository
{
    public Task<Restaurant?> GetByIdAsync(RestaurantId id, CancellationToken cancellationToken) =>
        dbContext.Restaurants.Include(r => r.Menu).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Restaurant>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Restaurants.Include(r => r.Menu).ToListAsync(cancellationToken);

    public void Add(Restaurant restaurant) => dbContext.Restaurants.Add(restaurant);
}
