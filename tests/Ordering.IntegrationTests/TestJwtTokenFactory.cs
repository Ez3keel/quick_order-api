using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ordering.IntegrationTests;

/// <summary>Mints tokens shaped like Identity.Api would, signed with the same test
/// key OrderingApiFactory configures. See Catalog.IntegrationTests' copy of this for
/// the full rationale — duplicated deliberately, same as the rest of this project's
/// "no shared code between services" stance (docs item 2).</summary>
public static class TestJwtTokenFactory
{
    public const string SigningKey = "test-signing-key-only-used-in-integration-tests-32bytes";

    public static string Create(string role, Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, role),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "quickorder-identity",
            audience: "quickorder",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
