namespace SaaSPlatform.Domain.Common;

public interface ITenantOwned
{
    Guid TenantId { get; }
}
