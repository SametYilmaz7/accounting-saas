namespace SaaSPlatform.WebApi.Tenancy;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentTenant currentTenant)
    {
        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var values))
        {
            await _next(context);
            return;
        }

        if (values.Count != 1 || !Guid.TryParse(values[0], out var tenantId) || tenantId == Guid.Empty)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Tenant identifier is invalid.",
                detail: "X-Tenant-Id must contain exactly one non-empty GUID.").ExecuteAsync(context);
            return;
        }

        currentTenant.SetTenant(tenantId);
        await _next(context);
    }
}
