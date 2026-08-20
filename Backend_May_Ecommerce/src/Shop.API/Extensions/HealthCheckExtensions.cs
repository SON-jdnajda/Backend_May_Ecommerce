using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Shop.API.Health;

namespace Shop.API.Extensions;

public static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddHealthProbes(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("postgres", tags: [ReadyTag])
            .AddCheck<CacheHealthCheck>("redis", tags: [ReadyTag]);

        return services;
    }

    public static WebApplication MapHealthProbes(this WebApplication app)
    {
        // Liveness answers "is the process wedged?" - it must NOT touch
        // dependencies, or a database blip restarts every healthy instance.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        // Readiness answers "can this instance serve traffic right now?"
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag)
        });

        return app;
    }
}
