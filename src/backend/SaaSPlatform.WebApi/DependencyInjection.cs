using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.Infrastructure.Persistence;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured with a non-blank PostgreSQL connection string.");
        }

        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(provider => provider.GetRequiredService<CurrentTenant>());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddDbContext<SaaSPlatformDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
