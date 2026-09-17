using Identity.Domain.Users;

namespace Identity.Api.Contracts;

public sealed record RegisterRequest(string Email, string Password, UserRole Role);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record RevokeRequest(string RefreshToken);
