namespace Identity.Application.Auth.Dtos;

public sealed record AuthResultDto(
    Guid UserId,
    string Email,
    string Role,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
