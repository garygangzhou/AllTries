using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TheSampleApi.HealthChecks;

public class UnHealthyHealthCheck : IHealthCheck    
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Unhealthy("This is testing unhealthy service."));
    }
}
