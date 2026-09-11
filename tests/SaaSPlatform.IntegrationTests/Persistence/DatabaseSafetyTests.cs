namespace SaaSPlatform.IntegrationTests.Persistence;

public sealed class DatabaseSafetyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Host=localhost")]
    [InlineData("Host=localhost;Database=saas_platform_dev")]
    [InlineData("Host=localhost;Database=postgres")]
    [InlineData("Host=localhost;Database=SAAS_PLATFORM_TEST")]
    [InlineData("Host=localhost;Database=production")]
    [InlineData("invalid-connection-string")]
    public void ConnectionString_WhenMissingOrUnsafe_ShouldRejectBeforeConnecting(string? value)
    {
        Assert.Throws<InvalidOperationException>(() => PostgreSqlFixture.ValidateConnectionString(value));
    }

    [Fact]
    public void ConnectionString_WhenDedicatedTestDatabase_ShouldAccept()
    {
        Assert.Equal("Host=localhost;Database=saas_platform_test",
            PostgreSqlFixture.ValidateConnectionString("Host=localhost;Database=saas_platform_test"));
    }
}
