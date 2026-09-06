using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Domain.Common;
using SaaSPlatform.Domain.Features.Tenants;

namespace SaaSPlatform.Infrastructure.Persistence;

public sealed class SaaSPlatformDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;

    public SaaSPlatformDbContext(
        DbContextOptions<SaaSPlatformDbContext> options,
        TimeProvider timeProvider) : base(options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaaSPlatformDbContext).Assembly);
    }

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
