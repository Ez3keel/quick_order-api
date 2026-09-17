using Identity.Domain.Common;
using Identity.Domain.Users;

namespace Identity.Domain.Tokens;

/// <summary>
/// References User by id only (see docs on aggregate references) — this is its own
/// aggregate so revoking or rotating a token never needs to load the whole User.
/// TokenHash, never the raw token: the raw refresh token only ever exists in the
/// response body and the client's storage, exactly like a password never being
/// stored in plaintext.
/// </summary>
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    public UserId UserId { get; private init; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset ExpiresAt { get; private init; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    private RefreshToken() { }

    private RefreshToken(RefreshTokenId id, UserId userId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Issue(UserId userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) =>
        new(RefreshTokenId.New(), userId, tokenHash, now, now + lifetime);

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    /// <summary>Called both on normal rotation (a new token was just issued to
    /// replace this one) and on logout/theft-response (no replacement).</summary>
    public void Revoke(DateTimeOffset now, RefreshTokenId? replacedByTokenId = null)
    {
        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }
}
