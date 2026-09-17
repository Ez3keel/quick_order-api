using System.Diagnostics;

namespace Catalog.Infrastructure.Observability;

/// <summary>One ActivitySource per service, by OpenTelemetry convention — Program.cs
/// registers this name with .AddSource(...) so spans started here actually get
/// exported instead of being silently dropped.</summary>
public static class CatalogActivitySource
{
    public const string Name = "QuickOrder.Catalog";

    public static readonly ActivitySource Instance = new(Name);
}
