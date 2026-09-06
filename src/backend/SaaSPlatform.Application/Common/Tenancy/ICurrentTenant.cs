namespace SaaSPlatform.Application.Common.Tenancy;

public interface ICurrentTenant
{
    bool IsResolved { get; }

    Guid? TenantId { get; }

    Guid GetRequiredTenantId();
}
