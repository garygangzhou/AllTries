using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TheSampleApi.HealthChecks;

public class RandomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        int randomResult = Random.Shared.Next(1, 4); //1, 2, 3,

        return randomResult switch
        {
            1 => Task.FromResult(HealthCheckResult.Healthy("This is testing random/healthy service.")),
            2 => Task.FromResult(HealthCheckResult.Degraded("This is testing random/degraded service.")),
            3 => Task.FromResult(HealthCheckResult.Unhealthy("This is testing random/unhealth service.")),
            _ => Task.FromResult(HealthCheckResult.Healthy("This is testing random/random service."))  // never happens, but just in case
        };
    }
}
