using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.Domain.Common;
using SaaSPlatform.Infrastructure.Persistence;

namespace SaaSPlatform.IntegrationTests.Persistence;

internal sealed class TenantOwnedTestDbContext(
    DbContextOptions<SaaSPlatformDbContext> options,
    ICurrentTenant tenant) : SaaSPlatformDbContext(options, TimeProvider.System, tenant)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<TenantOwnedTestRecord>();
        record.ToTable("integration_test_tenant_owned_records", "public");
        record.HasKey(entity => entity.Id);
        record.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        record.Property(entity => entity.TenantId).HasColumnName("tenant_id");
        base.OnModelCreating(modelBuilder);
    }
}

internal sealed class TenantOwnedTestRecord : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
