namespace Identity.Application.Abstractions;

/// <summary>
/// A refresh token is a high-entropy random value, not a password — hashing it is
/// about not leaving a bearer credential readable in the database (if the DB leaks,
/// the tokens in it shouldn't be directly usable), not about resisting guessing. A
/// fast cryptographic hash (SHA-256) is the right tool here, unlike passwords, which
/// need a slow, salted hash (BCrypt/PBKDF2, see IPasswordHasher) specifically because
/// humans pick guessable passwords and a fast hash would make offline guessing cheap.
/// </summary>
public interface IRefreshTokenHasher
{
    string Hash(string rawToken);
}
