using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.Domain.Common;
using SaaSPlatform.Domain.Features.Tenants;
using SaaSPlatform.Infrastructure.Persistence;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.UnitTests.Tenancy;

public sealed class TenantQueryFilterTests
{
    [Fact]
    public void Tenant_WhenContextIsMissing_ShouldRemainUnfiltered()
    {
        using var context = CreateContext(new CurrentTenant());

        Assert.Empty(context.Model.FindEntityType(typeof(Tenant))!.GetDeclaredQueryFilters());
        Assert.DoesNotContain("WHERE", context.Tenants.ToQueryString());
    }

    [Fact]
    public void TenantOwnedEntity_ShouldReceiveFilter()
    {
        using var context = CreateContext(new CurrentTenant());

        Assert.Single(context.Model.FindEntityType(typeof(TestRecord))!.GetDeclaredQueryFilters());
    }

    [Fact]
    public void TenantOwnedQuery_WhenUnresolved_ShouldFailClosed()
    {
        using var context = CreateContext(new CurrentTenant());

        Assert.Throws<TenantContextNotResolvedException>(() => context.Set<TestRecord>().ToQueryString());
    }

    [Fact]
    public void TenantOwnedQuery_WithSharedModel_ShouldUseEachContextsTenant()
    {
        var firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var firstTenant = new CurrentTenant();
        var secondTenant = new CurrentTenant();
        firstTenant.SetTenant(firstId);
        secondTenant.SetTenant(secondId);
        using var first = CreateContext(firstTenant);
        using var second = CreateContext(secondTenant);

        Assert.Same(first.Model, second.Model);
        var firstSql = first.Set<TestRecord>().ToQueryString();
        var secondSql = second.Set<TestRecord>().ToQueryString();

        Assert.Contains("WHERE", firstSql);
        Assert.Contains("WHERE", secondSql);
        Assert.Contains(firstId.ToString(), firstSql);
        Assert.DoesNotContain(secondId.ToString(), firstSql);
        Assert.Contains(secondId.ToString(), secondSql);
        Assert.DoesNotContain(firstId.ToString(), secondSql);
    }

    [Fact]
    public void TenantOwnedQuery_WhenResolvedAfterModelCreation_ShouldUseResolvedTenant()
    {
        var tenant = new CurrentTenant();
        using var context = CreateContext(tenant);
        _ = context.Model;
        var tenantId = Guid.NewGuid();

        tenant.SetTenant(tenantId);

        Assert.Contains(tenantId.ToString(), context.Set<TestRecord>().ToQueryString());
    }

    private static FilterTestDbContext CreateContext(ICurrentTenant tenant)
    {
        var options = new DbContextOptionsBuilder<SaaSPlatformDbContext>()
            .UseNpgsql()
            .Options;
        return new FilterTestDbContext(options, tenant);
    }

    private sealed class FilterTestDbContext(
        DbContextOptions<SaaSPlatformDbContext> options,
        ICurrentTenant tenant) : SaaSPlatformDbContext(options, TimeProvider.System, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestRecord>().HasKey(entity => entity.Id);
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class TestRecord : ITenantOwned
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
    }
}
