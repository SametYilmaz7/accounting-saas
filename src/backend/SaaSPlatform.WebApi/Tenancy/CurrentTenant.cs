using SaaSPlatform.Application.Common.Tenancy;

namespace SaaSPlatform.WebApi.Tenancy;

public sealed class CurrentTenant : ICurrentTenant
{
    private Guid? _tenantId;

    public bool IsResolved => _tenantId.HasValue;

    public Guid? TenantId => _tenantId;

    public Guid GetRequiredTenantId()
        => _tenantId ?? throw new TenantContextNotResolvedException();

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier must not be empty.", nameof(tenantId));
        }

        if (_tenantId.HasValue && _tenantId.Value != tenantId)
        {
            throw new InvalidOperationException("The current tenant cannot be changed once resolved.");
        }

        _tenantId = tenantId;
    }
}
