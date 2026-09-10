namespace SaaSPlatform.Application.Common.Tenancy;

public sealed class TenantIsolationViolationException : Exception
{
    public TenantIsolationViolationException()
        : base("The write violates the current tenant's ownership boundary.")
    {
    }
}
