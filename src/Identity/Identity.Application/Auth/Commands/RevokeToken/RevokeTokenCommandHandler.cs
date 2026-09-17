using Identity.Application.Abstractions;
using Identity.Application.Auth.Exceptions;
using MediatR;

namespace Identity.Application.Auth.Commands.RevokeToken;

/// <summary>Logout. Revoking without a replacement (no rotation happens here) — the
/// client is done, not asking for a new pair.</summary>
public sealed class RevokeTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IRefreshTokenHasher refreshTokenHasher,
    TimeProvider timeProvider) : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RawRefreshToken);
        var token = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        token.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
