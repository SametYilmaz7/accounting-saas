using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace SaaSPlatform.WebApi.Health;

public static class HealthEndpoints
{
    public static IEndpointConventionBuilder MapPlatformHealth(this IEndpointRouteBuilder endpoints)
        => endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(
                new { status = report.Status.ToString() }, context.RequestAborted)
        }).WithMetadata(new HttpMethodMetadata([HttpMethods.Get]));
}
