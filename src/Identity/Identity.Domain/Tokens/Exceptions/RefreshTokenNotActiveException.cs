namespace Identity.Domain.Tokens.Exceptions;

/// <summary>
/// Raised when a refresh token that's expired or already revoked is presented again.
/// A second use of an already-rotated token is the textbook sign of token theft
/// (someone replaying a refresh token the legitimate client already exchanged) —
/// the application layer reacts to this by revoking the whole chain, not just denying
/// the one request.
/// </summary>
public sealed class RefreshTokenNotActiveException : Exception
{
    public RefreshTokenId RefreshTokenId { get; }

    public RefreshTokenNotActiveException(RefreshTokenId refreshTokenId)
        : base($"Refresh token '{refreshTokenId}' is not active (expired or already revoked).")
    {
        RefreshTokenId = refreshTokenId;
    }
}
