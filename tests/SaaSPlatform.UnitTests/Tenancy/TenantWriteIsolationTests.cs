using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.Domain.Common;
using SaaSPlatform.Domain.Features.Tenants;
using SaaSPlatform.Infrastructure.Persistence;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.UnitTests.Tenancy;

public sealed class TenantWriteIsolationTests
{
    public static IEnumerable<object[]> WriteCases()
    {
        foreach (var mode in Enumerable.Range(0, 4))
        {
            foreach (var state in new[] { EntityState.Added, EntityState.Modified, EntityState.Deleted })
            {
                yield return [mode, state];
            }
        }
    }

    public static IEnumerable<object[]> ExistingWriteCases()
        => WriteCases().Where(testCase => (EntityState)testCase[1] != EntityState.Added);

    [Theory]
    [MemberData(nameof(WriteCases))]
    public async Task Save_WhenOwnershipMatches_ShouldReachDatabase(int mode, EntityState state)
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateContext(tenantId);
        var record = Track(context, tenantId, state);

        var exception = await Record.ExceptionAsync(() => Save(context, mode));

        Assert.NotNull(exception);
        Assert.IsType<DatabaseReachedException>(exception.GetBaseException());
        Assert.True(context.Connection.OpenAttempted);
        Assert.Equal(tenantId, record.TenantId);
    }

    [Theory]
    [MemberData(nameof(WriteCases))]
    public async Task Save_WhenOwnershipDiffers_ShouldRejectBeforeDatabase(int mode, EntityState state)
    {
        using var context = CreateContext(Guid.NewGuid());
        var otherId = Guid.NewGuid();
        var record = Track(context, otherId, state);

        await Assert.ThrowsAsync<TenantIsolationViolationException>(() => Save(context, mode));

        Assert.False(context.Connection.OpenAttempted);
        Assert.Equal(otherId, record.TenantId);
    }

    [Theory]
    [MemberData(nameof(WriteCases))]
    public async Task Save_WhenOwnershipIsEmpty_ShouldRejectWithoutAssignment(int mode, EntityState state)
    {
        using var context = CreateContext(Guid.NewGuid());
        var record = Track(context, Guid.Empty, state);

        await Assert.ThrowsAsync<TenantIsolationViolationException>(() => Save(context, mode));

        Assert.Equal(Guid.Empty, record.TenantId);
        Assert.False(context.Connection.OpenAttempted);
    }

    [Theory]
    [MemberData(nameof(WriteCases))]
    public async Task Save_WhenUnresolved_ShouldFailClosed(int mode, EntityState state)
    {
        using var context = CreateContext(null);
        Track(context, Guid.NewGuid(), state);

        await Assert.ThrowsAsync<TenantContextNotResolvedException>(() => Save(context, mode));

        Assert.False(context.Connection.OpenAttempted);
    }

    [Theory]
    [MemberData(nameof(ExistingWriteCases))]
    public async Task Save_WhenExistingOwnershipChangesToCurrentTenant_ShouldReject(int mode, EntityState state)
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateContext(tenantId);
        var record = Track(context, Guid.NewGuid(), EntityState.Unchanged);
        record.TenantId = tenantId;
        if (state == EntityState.Deleted)
        {
            context.Remove(record);
        }

        await Assert.ThrowsAsync<TenantIsolationViolationException>(() => Save(context, mode));

        Assert.False(context.Connection.OpenAttempted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Save_WhenUnchangedAndUnresolved_ShouldNotReject(int mode)
    {
        using var context = CreateContext(null);
        Track(context, Guid.NewGuid(), EntityState.Unchanged);

        Assert.Equal(0, await Save(context, mode));
        Assert.False(context.Connection.OpenAttempted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Save_WhenAuditableEntityAdded_ShouldSetBothUtcTimestamps(int mode)
    {
        using var context = CreateContext(null);
        var tenant = Tenant.Create(Guid.NewGuid(), "Workspace", "workspace");
        context.Add(tenant);

        var exception = await Record.ExceptionAsync(() => Save(context, mode));

        Assert.NotNull(exception);
        Assert.IsType<DatabaseReachedException>(exception.GetBaseException());
        Assert.Equal(context.Clock.GetUtcNow().UtcDateTime, tenant.CreatedAtUtc);
        Assert.Equal(tenant.CreatedAtUtc, tenant.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, tenant.UpdatedAtUtc.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Save_WhenAuditableEntityModified_ShouldPreserveCreationTime(int mode)
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateContext(tenantId);
        var record = Track(context, tenantId, EntityState.Unchanged);
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entry = context.Entry(record);
        entry.Property(entity => entity.CreatedAtUtc).CurrentValue = createdAt;
        entry.State = EntityState.Unchanged;
        entry.Property(entity => entity.CreatedAtUtc).OriginalValue = createdAt;
        entry.Property(entity => entity.CreatedAtUtc).CurrentValue = createdAt.AddDays(1);
        record.Value = "changed";

        var exception = await Record.ExceptionAsync(() => Save(context, mode));

        Assert.NotNull(exception);
        Assert.IsType<DatabaseReachedException>(exception.GetBaseException());
        Assert.Equal(createdAt, record.CreatedAtUtc);
        Assert.False(entry.Property(entity => entity.CreatedAtUtc).IsModified);
        Assert.Equal(context.Clock.GetUtcNow().UtcDateTime, record.UpdatedAtUtc);
    }

    private static Task<int> Save(SaaSPlatformDbContext context, int mode) => mode switch
    {
        0 => Task.FromResult(context.SaveChanges()),
        1 => Task.FromResult(context.SaveChanges(false)),
        2 => context.SaveChangesAsync(),
        _ => context.SaveChangesAsync(false)
    };

    private static TestRecord Track(WriteTestDbContext context, Guid tenantId, EntityState state)
    {
        var record = new TestRecord { TenantId = tenantId };
        context.Entry(record).State = state;
        return record;
    }

    private static WriteTestDbContext CreateContext(Guid? tenantId)
    {
        var tenant = new CurrentTenant();
        if (tenantId.HasValue)
        {
            tenant.SetTenant(tenantId.Value);
        }

        var connection = new ProbeConnection();
        var clock = new FixedTimeProvider();
        var options = new DbContextOptionsBuilder<SaaSPlatformDbContext>().UseNpgsql(connection).Options;
        return new WriteTestDbContext(options, clock, tenant, connection);
    }

    private sealed class WriteTestDbContext(
        DbContextOptions<SaaSPlatformDbContext> options,
        FixedTimeProvider clock,
        ICurrentTenant tenant,
        ProbeConnection connection) : SaaSPlatformDbContext(options, clock, tenant)
    {
        public ProbeConnection Connection { get; } = connection;
        public FixedTimeProvider Clock { get; } = clock;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestRecord>().HasKey(entity => entity.Id);
            modelBuilder.Entity<TestRecord>().Property(entity => entity.Id).ValueGeneratedNever();
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class TestRecord : AuditableEntity, ITenantOwned
    {
        public TestRecord() => Id = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Value { get; set; } = "initial";
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class DatabaseReachedException : Exception;

    private sealed class ProbeConnection : DbConnection
    {
        public bool OpenAttempted { get; private set; }
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "probe";
        public override string DataSource => "probe";
        public override string ServerVersion => "17.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void Open()
        {
            OpenAttempted = true;
            throw new DatabaseReachedException();
        }
        public override Task OpenAsync(CancellationToken cancellationToken) { Open(); return Task.CompletedTask; }
        public override void Close() { }
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
