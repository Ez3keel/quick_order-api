using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Identity.IntegrationTests;

public sealed class IdentityApiFactory : WebApplicationFactory<Identity.Api.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    /// <summary>High by default so functional tests (many requests against the same
    /// rate-limited endpoints from one shared HttpClient) don't trip the limiter —
    /// RateLimitingTests overrides this back down to exercise the limiter itself.</summary>
    public int AuthRateLimitPermitLimit { get; init; } = 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-only-used-in-integration-tests-32bytes");
        builder.UseSetting("RateLimiting:Auth:PermitLimit", AuthRateLimitPermitLimit.ToString());
        builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
