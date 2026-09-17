using Identity.Domain.Tokens;
using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    /// <summary>Looked up by the hash of the raw token the client presents — the
    /// raw value is never stored, so lookup has to hash first (see IRefreshTokenHasher).</summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Used for the theft-response: if a token that was already rotated gets
    /// presented again, every other still-active token for that user is revoked too,
    /// forcing a fresh login everywhere instead of trusting the rest of the chain.</summary>
    Task<IReadOnlyCollection<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
