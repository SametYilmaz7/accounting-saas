namespace SaaSPlatform.Application.Common.Tenancy;

public sealed class TenantContextNotResolvedException : Exception
{
    public TenantContextNotResolvedException()
        : base("The current tenant has not been resolved.")
    {
    }
}
