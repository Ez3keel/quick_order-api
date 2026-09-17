using Identity.Domain.Tokens;
using Identity.Domain.Users;

namespace Identity.Domain.Tests.Tokens;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_CreatesActiveTokenExpiringAfterLifetime()
    {
        var token = RefreshToken.Issue(UserId.New(), "hash", Now, TimeSpan.FromDays(7));

        Assert.True(token.IsActive(Now));
        Assert.Equal(Now.AddDays(7), token.ExpiresAt);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        var token = RefreshToken.Issue(UserId.New(), "hash", Now, TimeSpan.FromDays(7));

        Assert.False(token.IsActive(Now.AddDays(8)));
    }

    [Fact]
    public void Revoke_MarksTokenInactive()
    {
        var token = RefreshToken.Issue(UserId.New(), "hash", Now, TimeSpan.FromDays(7));

        token.Revoke(Now.AddHours(1));

        Assert.False(token.IsActive(Now.AddHours(2)));
        Assert.Equal(Now.AddHours(1), token.RevokedAt);
    }

    [Fact]
    public void Revoke_WithReplacement_RecordsTheReplacementTokenId()
    {
        var token = RefreshToken.Issue(UserId.New(), "hash", Now, TimeSpan.FromDays(7));
        var replacementId = RefreshTokenId.New();

        token.Revoke(Now.AddHours(1), replacementId);

        Assert.Equal(replacementId, token.ReplacedByTokenId);
    }
}
