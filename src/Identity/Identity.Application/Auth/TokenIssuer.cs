using System.Security.Cryptography;
using Identity.Application.Abstractions;
using Identity.Application.Auth.Dtos;
using Identity.Domain.Tokens;
using Identity.Domain.Users;

namespace Identity.Application.Auth;

/// <summary>
/// Shared by RegisterUser, Login and RefreshToken — "issue a fresh access+refresh
/// pair for this user" is the same operation regardless of what triggered it. Doesn't
/// call SaveChanges itself: the caller controls the transaction, since rotation needs
/// the old token's revocation and the new token's creation to land atomically together.
/// </summary>
public sealed class TokenIssuer(
    IAccessTokenGenerator accessTokenGenerator,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenHasher refreshTokenHasher,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public (AuthResultDto Result, RefreshTokenId RefreshTokenId) IssueFor(User user)
    {
        var now = timeProvider.GetUtcNow();
        var (accessToken, accessTokenExpiresAt) = accessTokenGenerator.Generate(user.Id, user.Email, user.Role);

        var rawRefreshToken = GenerateRawToken();
        var refreshToken = RefreshToken.Issue(user.Id, refreshTokenHasher.Hash(rawRefreshToken), now, RefreshTokenLifetime);
        refreshTokenRepository.Add(refreshToken);

        var result = new AuthResultDto(
            user.Id.Value,
            user.Email,
            user.Role.ToString(),
            accessToken,
            accessTokenExpiresAt,
            rawRefreshToken,
            refreshToken.ExpiresAt);

        return (result, refreshToken.Id);
    }

    private static string GenerateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
