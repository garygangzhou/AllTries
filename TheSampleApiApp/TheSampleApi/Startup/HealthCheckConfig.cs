using TheSampleApi.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

namespace TheSampleApi.Startup;

public static class HealthCheckConfig
{
    public static void AddAllHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<RandomHealthCheck>("Random Health Check", tags: ["random"])
            .AddCheck<HealthHealthyCheck>("Healthy Health Check", tags: ["healthy"])
            .AddCheck<DegradedHealthCheck>("Degraded Health Check", tags: ["degraded"])
            .AddCheck<UnHealthyHealthCheck>("Unhealthy Health Check", tags: ["unhealthy"]);
    }
    public static void UseHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/healthy", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("healthy")
        });
        app.MapHealthChecks("/health/unhealthy", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("unhealthy")
        });
        app.MapHealthChecks("/health/degraded", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("degraded")
        });
        app.MapHealthChecks("/health/random", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("random")
        });

        app.MapHealthChecks("/health/ui", new HealthCheckOptions { 
           ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ui/healthy", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("healthy"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecks("/health/ui/unhealthy", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("unhealthy"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecks("/health/ui/degraded", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("degraded"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecks("/health/ui/random", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("random"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }
}
