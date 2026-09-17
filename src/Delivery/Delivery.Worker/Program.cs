using Delivery.Application;
using Microsoft.AspNetCore.Builder;
using Delivery.Infrastructure;
using Delivery.Infrastructure.Observability;
using Delivery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// A WebApplication, not a plain Host — the worker itself is still just background
// services (OutboxPublisher, OrderReadyForAssignmentConsumer, registered below), but
// exposing /metrics for Prometheus to scrape needs *some* HTTP listener, and adding
// one small endpoint here is simpler than running a second process just for metrics.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDeliveryApplication();
builder.Services.AddDeliveryInfrastructure(builder.Configuration);
builder.Services.AddDeliveryAssignmentConsumer();

const string serviceName = "quickorder-delivery-worker";
var otlpEndpoint = builder.Configuration["Otel:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(DeliveryActivitySource.Name)
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        if (!string.IsNullOrEmpty(otlpEndpoint))
            tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapPrometheusScrapingEndpoint();

app.Run();

namespace Delivery.Worker
{
    public partial class Program;
}
