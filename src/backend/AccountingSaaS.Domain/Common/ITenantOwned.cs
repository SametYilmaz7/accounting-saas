namespace AccountingSaaS.Domain.Common;

public interface ITenantOwned
{
    Guid TenantId { get; }
}
