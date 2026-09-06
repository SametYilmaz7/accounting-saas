using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.UnitTests.Tenancy;

public sealed class CurrentTenantTests
{
    [Fact]
    public void IsResolved_WhenNew_ShouldBeFalse()
    {
        Assert.False(new CurrentTenant().IsResolved);
    }

    [Fact]
    public void TenantId_WhenNew_ShouldBeNull()
    {
        Assert.Null(new CurrentTenant().TenantId);
    }

    [Fact]
    public void GetRequiredTenantId_WhenUnresolved_ShouldThrow()
    {
        ICurrentTenant context = new CurrentTenant();

        Assert.Throws<TenantContextNotResolvedException>(() => context.GetRequiredTenantId());
    }

    [Fact]
    public void SetTenant_WhenValid_ShouldResolveContext()
    {
        var context = new CurrentTenant();
        var tenantId = Guid.NewGuid();

        context.SetTenant(tenantId);

        Assert.True(context.IsResolved);
        Assert.Equal(tenantId, context.TenantId);
    }

    [Fact]
    public void GetRequiredTenantId_WhenResolved_ShouldReturnTenantId()
    {
        var context = new CurrentTenant();
        var tenantId = Guid.NewGuid();
        context.SetTenant(tenantId);

        Assert.Equal(tenantId, context.GetRequiredTenantId());
    }

    [Fact]
    public void SetTenant_WhenEmpty_ShouldRejectAndRemainUnresolved()
    {
        var context = new CurrentTenant();

        Assert.Throws<ArgumentException>("tenantId", () => context.SetTenant(Guid.Empty));
        Assert.False(context.IsResolved);
        Assert.Null(context.TenantId);
    }

    [Fact]
    public void SetTenant_WhenSameTenantIsSetTwice_ShouldBeIdempotent()
    {
        var context = new CurrentTenant();
        var tenantId = Guid.NewGuid();
        context.SetTenant(tenantId);

        context.SetTenant(tenantId);

        Assert.True(context.IsResolved);
        Assert.Equal(tenantId, context.GetRequiredTenantId());
    }

    [Fact]
    public void SetTenant_WhenDifferentTenantIsSet_ShouldRejectAndPreserveTenant()
    {
        var context = new CurrentTenant();
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        context.SetTenant(tenantId);

        Assert.Throws<InvalidOperationException>(() => context.SetTenant(otherTenantId));
        Assert.Equal(tenantId, context.GetRequiredTenantId());
    }

    [Fact]
    public void SetTenant_WhenEmptyAfterResolution_ShouldRejectAndPreserveTenant()
    {
        var context = new CurrentTenant();
        var tenantId = Guid.NewGuid();
        context.SetTenant(tenantId);

        Assert.Throws<ArgumentException>("tenantId", () => context.SetTenant(Guid.Empty));
        Assert.Equal(tenantId, context.GetRequiredTenantId());
    }

    [Fact]
    public void SetTenant_WhenOneInstanceResolves_ShouldNotResolveAnother()
    {
        var first = new CurrentTenant();
        var second = new CurrentTenant();

        first.SetTenant(Guid.NewGuid());

        Assert.False(second.IsResolved);
        Assert.Null(second.TenantId);
        Assert.Throws<TenantContextNotResolvedException>(() => second.GetRequiredTenantId());
    }
}
