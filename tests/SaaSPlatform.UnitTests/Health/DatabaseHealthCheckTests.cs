using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SaaSPlatform.Infrastructure.Persistence;
using SaaSPlatform.WebApi.Health;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.UnitTests.Health;

public sealed class DatabaseHealthCheckTests
{
    [Theory]
    [InlineData(true, HealthStatus.Healthy)]
    [InlineData(false, HealthStatus.Unhealthy)]
    public async Task CheckHealthAsync_ShouldReportConnectivityAndForwardCancellation(
        bool canConnect, HealthStatus expectedStatus)
    {
        using var cancellation = new CancellationTokenSource();
        using var dbContext = new StubDbContext(token =>
        {
            Assert.Equal(cancellation.Token, token);
            return Task.FromResult(canConnect);
        });

        var result = await new DatabaseHealthCheck(dbContext)
            .CheckHealthAsync(new HealthCheckContext(), cancellation.Token);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.Exception);
        if (!canConnect)
        {
            Assert.Equal("Database is unreachable.", result.Description);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectivityThrows_ShouldRetainExceptionWithSafeDescription()
    {
        var exception = new InvalidOperationException("Sensitive database details");
        using var dbContext = new StubDbContext(_ => Task.FromException<bool>(exception));

        var result = await new DatabaseHealthCheck(dbContext).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Same(exception, result.Exception);
        Assert.Equal("Database connectivity check failed.", result.Description);
    }

    private sealed class StubDbContext : SaaSPlatformDbContext
    {
        private readonly DatabaseFacade _database;

        public StubDbContext(Func<CancellationToken, Task<bool>> canConnect)
            : base(new DbContextOptions<SaaSPlatformDbContext>(), TimeProvider.System, new CurrentTenant())
        {
            _database = new StubDatabaseFacade(this, canConnect);
        }

        public override DatabaseFacade Database => _database;
    }

    private sealed class StubDatabaseFacade(DbContext context, Func<CancellationToken, Task<bool>> canConnect)
        : DatabaseFacade(context)
    {
        public override Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
            => canConnect(cancellationToken);
    }
}
