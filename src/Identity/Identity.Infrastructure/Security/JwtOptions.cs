namespace Identity.Infrastructure.Security;

/// <summary>
/// The signing key here and the one every other service uses to *validate* tokens
/// (their own copy of the same value, in their own config — see docs item 36) must be
/// identical. A symmetric HMAC key is the simplest thing that works for a project this
/// size; a production system with truly independent deploy pipelines per service would
/// use asymmetric signing (RS256) and publish a JWKS endpoint instead of distributing
/// a shared secret through config in N places.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "quickorder-identity";
    public string Audience { get; set; } = "quickorder";
    public string SigningKey { get; set; } = null!;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
}
