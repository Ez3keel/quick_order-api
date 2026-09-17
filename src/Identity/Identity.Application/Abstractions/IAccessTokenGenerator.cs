using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IAccessTokenGenerator
{
    /// <summary>Returns the signed JWT and when it expires. Claims carry UserId, Email
    /// and Role — every other service in the system validates this token against a
    /// shared signing key and trusts these claims without calling back to Identity.</summary>
    (string Token, DateTimeOffset ExpiresAt) Generate(UserId userId, string email, UserRole role);
}
