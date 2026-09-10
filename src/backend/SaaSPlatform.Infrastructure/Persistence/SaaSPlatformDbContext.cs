using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.Domain.Common;
using SaaSPlatform.Domain.Features.Tenants;

namespace SaaSPlatform.Infrastructure.Persistence;

public class SaaSPlatformDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentTenant _currentTenant;

    public SaaSPlatformDbContext(
        DbContextOptions<SaaSPlatformDbContext> options,
        TimeProvider timeProvider,
        ICurrentTenant currentTenant) : base(options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        ArgumentNullException.ThrowIfNull(currentTenant);
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    private Guid RequiredTenantId => _currentTenant.GetRequiredTenantId();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaaSPlatformDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            if (entityType.BaseType is not null)
            {
                if (!typeof(ITenantOwned).IsAssignableFrom(entityType.GetRootType().ClrType))
                {
                    throw new InvalidOperationException("Tenant-owned inheritance requires a tenant-owned root entity.");
                }

                continue;
            }

            var entity = Expression.Parameter(entityType.ClrType, "entity");
            Expression<Func<Guid>> requiredTenantId = () => RequiredTenantId;
            var predicate = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(entity, nameof(ITenantOwned.TenantId)),
                    requiredTenantId.Body),
                entity);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter("TenantIsolation", predicate);
        }
    }

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites();
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateTenantWrites();
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTenantWrites()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var currentTenantId = _currentTenant.GetRequiredTenantId();
            var tenantId = entry.Property(entity => entity.TenantId);

            if (tenantId.CurrentValue == Guid.Empty || tenantId.CurrentValue != currentTenantId)
            {
                throw new TenantIsolationViolationException();
            }

            if (entry.State is EntityState.Modified or EntityState.Deleted
                && tenantId.OriginalValue != tenantId.CurrentValue)
            {
                throw new TenantIsolationViolationException();
            }
        }
    }

    private void ApplyAuditTimestamps()
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAtUtc).CurrentValue = utcNow;
                entry.Property(entity => entity.UpdatedAtUtc).CurrentValue = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                var createdAt = entry.Property(entity => entity.CreatedAtUtc);
                createdAt.CurrentValue = createdAt.OriginalValue;
                createdAt.IsModified = false;
                entry.Property(entity => entity.UpdatedAtUtc).CurrentValue = utcNow;
                entry.Property(entity => entity.UpdatedAtUtc).IsModified = true;
            }
        }
    }
}
