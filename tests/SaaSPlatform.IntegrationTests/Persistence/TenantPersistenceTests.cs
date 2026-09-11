using Microsoft.EntityFrameworkCore;
using Npgsql;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.Domain.Features.Tenants;
using SaaSPlatform.Infrastructure.Persistence;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class TenantPersistenceTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private NpgsqlConnection _connection = null!;
    private NpgsqlTransaction _transaction = null!;

    public async Task InitializeAsync()
    {
        _connection = await fixture.OpenConnectionAsync();
        _transaction = await _connection.BeginTransactionAsync();
        // Regular table permits the FK to public.tenants; transactional DDL is removed by rollback.
        await using var command = new NpgsqlCommand("""
            CREATE TABLE public.integration_test_tenant_owned_records (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL REFERENCES public.tenants(id)
            )
            """, _connection, _transaction);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }
        }
        finally
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
    }

    private SaaSPlatformDbContext CreateContext(Guid? tenantId = null, bool withTestRecords = false)
    {
        var tenant = new CurrentTenant();
        if (tenantId.HasValue)
        {
            tenant.SetTenant(tenantId.Value);
        }

        var options = new DbContextOptionsBuilder<SaaSPlatformDbContext>().UseNpgsql(_connection).Options;
        SaaSPlatformDbContext context = withTestRecords
            ? new TenantOwnedTestDbContext(options, tenant)
            : new SaaSPlatformDbContext(options, TimeProvider.System, tenant);
        context.Database.UseTransaction(_transaction);
        return context;
    }

    private async Task<Guid> AddTenantAsync()
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        context.Tenants.Add(Tenant.Create(id, "Test Workspace", $"workspace-{id:N}"));
        await context.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task Migrations_ShouldBeAppliedAndTenantsTableQueryable()
    {
        await using var context = CreateContext();
        Assert.Contains("20260910194820_InitialCreate", await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        await context.Tenants.Take(1).ToListAsync();
    }

    [Fact]
    public async Task Tenant_ShouldRoundTripWithUtcAuditFieldsWithoutTenantContext()
    {
        var before = DateTime.UtcNow;
        var id = await AddTenantAsync();
        await using var context = CreateContext();
        var tenant = await context.Tenants.SingleAsync(entity => entity.Id == id);
        Assert.Equal("Test Workspace", tenant.Name);
        Assert.Equal($"workspace-{id:N}", tenant.Slug);
        Assert.True(tenant.IsActive);
        Assert.InRange(tenant.CreatedAtUtc, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, tenant.CreatedAtUtc.Kind);
        Assert.Equal(tenant.CreatedAtUtc, tenant.UpdatedAtUtc);
    }

    [Fact]
    public async Task Tenant_WhenIsActiveOmitted_ShouldUsePostgreSqlDefault()
    {
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        // Direct SQL intentionally exercises the database default independently of Tenant.Create.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.tenants (id, name, slug, created_at_utc, updated_at_utc)
            VALUES ({id}, {"Default Workspace"}, {$"default-{id:N}"}, {now}, {now})
            """);
        Assert.True((await context.Tenants.SingleAsync(entity => entity.Id == id)).IsActive);
    }

    [Fact]
    public async Task TenantOwnedQueries_ShouldIsolateBothTenantsAcrossContexts()
    {
        var firstId = await AddTenantAsync();
        var secondId = await AddTenantAsync();
        var firstRecordId = await AddRecordAsync(firstId);
        var secondRecordId = await AddRecordAsync(secondId);
        await using var first = CreateContext(firstId, true);
        await using var second = CreateContext(secondId, true);
        Assert.Equal(firstRecordId, Assert.Single(await first.Set<TenantOwnedTestRecord>().ToListAsync()).Id);
        Assert.Equal(secondRecordId, Assert.Single(await second.Set<TenantOwnedTestRecord>().ToListAsync()).Id);
    }

    [Fact]
    public async Task TenantOwnedQuery_WhenUnresolved_ShouldFailClosed()
    {
        await AddRecordAsync(await AddTenantAsync());
        await using var context = CreateContext(withTestRecords: true);
        await Assert.ThrowsAsync<TenantContextNotResolvedException>(() => context.Set<TenantOwnedTestRecord>().ToListAsync());
    }

    private async Task<Guid> AddRecordAsync(Guid tenantId)
    {
        await using var context = CreateContext(tenantId, true);
        var record = new TenantOwnedTestRecord { Id = Guid.NewGuid(), TenantId = tenantId };
        context.Add(record);
        Assert.Equal(1, await context.SaveChangesAsync());
        return record.Id;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenantOwnedWrite_WhenMismatchedOrEmpty_ShouldReject(bool empty)
    {
        var tenantId = await AddTenantAsync();
        var otherId = empty ? Guid.Empty : await AddTenantAsync();
        await using var context = CreateContext(tenantId, true);
        context.Add(new TenantOwnedTestRecord { Id = Guid.NewGuid(), TenantId = otherId });
        await Assert.ThrowsAsync<TenantIsolationViolationException>(() => context.SaveChangesAsync());
        await using var verification = CreateContext(tenantId, true);
        Assert.Empty(await verification.Set<TenantOwnedTestRecord>().ToListAsync());
    }

    [Fact]
    public async Task TenantOwnedWrite_WhenTenantIdChanges_ShouldRejectAndPreserveStoredOwner()
    {
        var firstId = await AddTenantAsync();
        var secondId = await AddTenantAsync();
        var recordId = await AddRecordAsync(firstId);
        await using var context = CreateContext(firstId, true);
        var record = await context.Set<TenantOwnedTestRecord>().SingleAsync();
        record.TenantId = secondId;
        await Assert.ThrowsAsync<TenantIsolationViolationException>(() => context.SaveChangesAsync());
        await using var verification = CreateContext(firstId, true);
        Assert.Equal(firstId, (await verification.Set<TenantOwnedTestRecord>().SingleAsync(entity => entity.Id == recordId)).TenantId);
    }

    [Fact]
    public async Task Tenant_WhenSlugDuplicated_ShouldBeRejectedByPostgreSql()
    {
        var id = await AddTenantAsync();
        await using var context = CreateContext();
        context.Add(Tenant.Create(Guid.NewGuid(), "Duplicate Workspace", $"workspace-{id:N}"));
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("uq_tenants_slug", postgres.ConstraintName);
    }

    [Fact]
    public async Task Tenant_WhenInvalidSlugSentDirectly_ShouldBeRejectedByPostgreSql()
    {
        var id = await AddTenantAsync();
        await using var context = CreateContext();
        // Deliberate invalid SQL input tests the DB boundary without changing Domain rules.
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE public.tenants SET slug = {"INVALID SLUG"} WHERE id = {id}"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_tenants_slug_format", exception.ConstraintName);
    }
}
