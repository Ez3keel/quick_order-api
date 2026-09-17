using System.Threading.RateLimiting;
using Identity.Api.Middleware;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Auth endpoints are the classic brute-force target (credential stuffing against
// /login, spamming /register). A tight fixed-window limit per client IP is a cheap,
// meaningful first line of defense — it doesn't replace account lockout or CAPTCHA
// for a production system, but it stops the naive automated attempt.
// PermitLimit/WindowSeconds are configurable (not hardcoded) so integration tests can
// raise the budget for functional tests and keep it tight only where they mean to
// exercise the limiter itself — see docs item 38.
var authRateLimitPermitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 5);
var authRateLimitWindowSeconds = builder.Configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Partitioned by caller IP, not a single shared bucket — otherwise one busy
    // legitimate client could exhaust the limit for everyone else hitting /login.
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authRateLimitPermitLimit,
            Window = TimeSpan.FromSeconds(authRateLimitWindowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.MapControllers();

app.Run();

namespace Identity.Api
{
    public partial class Program;
}
