using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LoLCoach.Api.Infrastructure;

public sealed class DatabaseReadinessHealthCheck(PlayerDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool canConnect;
        try
        {
            canConnect = await db.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable.");
        }

        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
            : HealthCheckResult.Unhealthy("PostgreSQL is unreachable.");
    }
}
