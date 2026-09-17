using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    void Add(User user);
}
