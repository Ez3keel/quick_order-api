using Identity.Application.Abstractions;
using Identity.Application.Auth.Dtos;
using Identity.Application.Auth.Exceptions;
using MediatR;

namespace Identity.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Rotation: every refresh consumes the presented token and issues a brand new pair —
/// a refresh token is single-use. If a token that's already been consumed (revoked)
/// shows up again, that's the standard signal of token theft (an attacker replaying a
/// token the legitimate client already exchanged for a newer one), and the response is
/// to revoke every other active token for that user, not just deny this one request.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IRefreshTokenHasher refreshTokenHasher,
    TokenIssuer tokenIssuer,
    TimeProvider timeProvider) : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RawRefreshToken);
        var presentedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var now = timeProvider.GetUtcNow();

        if (!presentedToken.IsActive(now))
        {
            if (presentedToken.RevokedAt is not null)
                await RevokeAllActiveTokensForUserAsync(presentedToken.UserId, now, cancellationToken);

            throw new InvalidRefreshTokenException();
        }

        var user = await userRepository.GetByIdAsync(presentedToken.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var (result, newRefreshTokenId) = tokenIssuer.IssueFor(user);
        presentedToken.Revoke(now, newRefreshTokenId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task RevokeAllActiveTokensForUserAsync(
        Domain.Users.UserId userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var activeTokens = await refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var token in activeTokens)
            token.Revoke(now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
