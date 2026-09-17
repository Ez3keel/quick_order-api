using Identity.Domain.Users;
using Identity.Domain.Users.Exceptions;

namespace Identity.Domain.Tests.Users;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WithValidData_NormalizesEmailToLowercaseAndTrimmed()
    {
        var user = User.Register("  Someone@Example.com ", "hashed", UserRole.Customer, Now);

        Assert.Equal("someone@example.com", user.Email);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("")]
    public void Register_WithInvalidEmail_ThrowsInvalidEmailException(string email)
    {
        Assert.Throws<InvalidEmailException>(() => User.Register(email, "hashed", UserRole.Customer, Now));
    }

    [Fact]
    public void Register_WithBlankPasswordHash_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => User.Register("valid@example.com", " ", UserRole.Customer, Now));
    }
}
