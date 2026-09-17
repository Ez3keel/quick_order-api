namespace Identity.Application.Auth.Exceptions;

/// <summary>Deliberately as vague as InvalidCredentialsException — "token not found"
/// vs. "token expired" vs. "token already used" are all collapsed into one message so
/// a caller probing the endpoint can't distinguish them.</summary>
public sealed class InvalidRefreshTokenException() : Exception("Refresh token is invalid or expired.");
