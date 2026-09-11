using Microsoft.EntityFrameworkCore;
using Npgsql;
using SaaSPlatform.Infrastructure.Persistence;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.IntegrationTests.Persistence;

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _connectionString = ValidateConnectionString(
            Environment.GetEnvironmentVariable("ConnectionStrings__IntegrationTestDatabase"));
        await using var connection = await OpenConnectionAsync();
        await using var context = new SaaSPlatformDbContext(
            new DbContextOptionsBuilder<SaaSPlatformDbContext>().UseNpgsql(connection).Options,
            TimeProvider.System, new CurrentTenant());
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    internal static string ValidateConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("ConnectionStrings__IntegrationTestDatabase is required.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(value);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Integration test connection string is invalid.");
        }

        if (!string.Equals(builder.Database, "saas_platform_test", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Integration tests require database saas_platform_test.");
        }

        return builder.ConnectionString;
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ValidateConnectionString(_connectionString));
        try
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT current_database()", connection);
            if (!string.Equals(await command.ExecuteScalarAsync() as string, "saas_platform_test", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Connected database must be saas_platform_test.");
            }

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
