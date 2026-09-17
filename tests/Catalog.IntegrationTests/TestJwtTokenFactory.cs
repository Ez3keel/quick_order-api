using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.IntegrationTests;

/// <summary>
/// Mints tokens shaped exactly like Identity.Api would, signed with the same test key
/// CatalogApiFactory configures — this service only ever validates a signature and
/// reads claims, so a self-signed token with the right key is indistinguishable from
/// a real one for testing purposes (no need to spin up Identity.Api here too).
/// </summary>
public static class TestJwtTokenFactory
{
    public const string SigningKey = "test-signing-key-only-used-in-integration-tests-32bytes";

    public static string Create(string role, Guid? userId = null)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
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
