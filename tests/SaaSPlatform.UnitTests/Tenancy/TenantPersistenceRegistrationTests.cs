using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.Infrastructure.Persistence;
using SaaSPlatform.WebApi;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.UnitTests.Tenancy;

public sealed class TenantPersistenceRegistrationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void AddTenantPersistence_WhenConnectionMissingOrBlank_ShouldFailImmediately(string? value)
    {
        var services = new ServiceCollection();
        var configuration = Configuration(value);

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddTenantPersistence(configuration));

        Assert.Contains("ConnectionStrings:DefaultConnection", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddTenantPersistence_ShouldShareScopedTenantAndRegisterPostgreSql()
    {
        const string connectionString = "Host=localhost;Database=saas_platform_dev";
        var services = new ServiceCollection();
        services.AddTenantPersistence(Configuration(connectionString));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var current = first.ServiceProvider.GetRequiredService<CurrentTenant>();
        var abstraction = first.ServiceProvider.GetRequiredService<ICurrentTenant>();
        Assert.Same(current, abstraction);
        var tenantId = Guid.NewGuid();
        current.SetTenant(tenantId);
        Assert.Equal(tenantId, abstraction.GetRequiredTenantId());
        Assert.NotSame(current, second.ServiceProvider.GetRequiredService<CurrentTenant>());
        Assert.False(second.ServiceProvider.GetRequiredService<ICurrentTenant>().IsResolved);

        var dbContext = first.ServiceProvider.GetRequiredService<SaaSPlatformDbContext>();
        Assert.Same(dbContext, first.ServiceProvider.GetRequiredService<SaaSPlatformDbContext>());
        Assert.NotSame(dbContext, second.ServiceProvider.GetRequiredService<SaaSPlatformDbContext>());
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", dbContext.Database.ProviderName);
        Assert.Equal(connectionString, dbContext.Database.GetConnectionString());
        Assert.Same(TimeProvider.System, first.ServiceProvider.GetRequiredService<TimeProvider>());
        Assert.Same(TimeProvider.System, second.ServiceProvider.GetRequiredService<TimeProvider>());
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<CurrentTenant>());
    }

    private static IConfiguration Configuration(string? value)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = value
        }).Build();
}
