using System.Reflection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Shop.Application.Common.Diagnostics;

namespace Shop.API.Extensions;

public static class ObservabilityExtensions
{
    private const string DefaultServiceName = "shop-api";

    /// <summary>
    /// Latency buckets in seconds. The OpenTelemetry defaults are tuned for
    /// generic workloads; these are tightened around the 5ms-1s band where an
    /// HTTP API actually lives, so p95/p99 land on a real bucket edge instead
    /// of being interpolated across a 5-second gap.
    /// </summary>
    private static readonly double[] LatencyBuckets =
        [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 10];

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var serviceName = configuration["Observability:ServiceName"] ?? DefaultServiceName;
        var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        services.AddOpenTelemetry()
            // Without this every series is tagged "unknown_service:Shop.API",
            // which makes metrics from two services indistinguishable.
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName
                }))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Business metrics only reach the exporter if their Meter
                    // is opted in by name - instruments are not auto-discovered.
                    .AddMeter(ShopMeters.Orders)
                    .AddView(
                        "http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration { Boundaries = LatencyBuckets })
                    .AddPrometheusExporter();
            });

        return services;
    }
}
