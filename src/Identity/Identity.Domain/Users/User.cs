using System.Text.RegularExpressions;
using Identity.Domain.Common;
using Identity.Domain.Users.Exceptions;

namespace Identity.Domain.Users;

public sealed partial class User : Entity<UserId>
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }

    private User() { }

    private User(UserId id, string email, string passwordHash, UserRole role, DateTimeOffset now) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = now;
    }

    public static User Register(string email, string passwordHash, UserRole role, DateTimeOffset now)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!EmailPattern().IsMatch(normalizedEmail))
            throw new InvalidEmailException(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new User(UserId.New(), normalizedEmail, passwordHash, role, now);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
