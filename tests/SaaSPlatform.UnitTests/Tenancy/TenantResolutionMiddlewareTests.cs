using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SaaSPlatform.Application.Common.Tenancy;
using SaaSPlatform.WebApi;
using SaaSPlatform.WebApi.Tenancy;

namespace SaaSPlatform.UnitTests.Tenancy;

public sealed class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task MissingHeader_ShouldContinueWithUnresolvedTenant()
    {
        await VerifyRequestAsync(null, null, true);
    }

    [Fact]
    public async Task ValidHeader_ShouldResolveExactTenantInExistingScopeBeforeNext()
    {
        var tenantId = Guid.NewGuid();
        await VerifyRequestAsync(new StringValues(tenantId.ToString()), tenantId, true);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("3f54449e-9b42-4e32-92ab-7f52b6e8724a,3f54449e-9b42-4e32-92ab-7f52b6e8724a")]
    public async Task InvalidHeader_ShouldReturnBadRequestWithoutCallingNext(string value)
    {
        await VerifyRequestAsync(new StringValues(value), null, false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MultipleValues_ShouldRejectEvenWhenIdentical(bool identical)
    {
        var first = Guid.NewGuid().ToString();
        var second = identical ? first : Guid.NewGuid().ToString();
        await VerifyRequestAsync(new StringValues(new[] { first, second }), null, false);
    }

    private static async Task VerifyRequestAsync(StringValues? header, Guid? expectedTenantId, bool shouldCallNext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTenantPersistence(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=saas_platform_test"
            }).Build());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<CurrentTenant>();
        using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Response.Body = body;
        if (header.HasValue)
        {
            context.Request.Headers["X-Tenant-Id"] = header.Value;
        }

        var nextCalled = false;
        var app = new ApplicationBuilder(provider);
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.Run(httpContext =>
        {
            nextCalled = true;
            Assert.Same(currentTenant, httpContext.RequestServices.GetRequiredService<ICurrentTenant>());
            Assert.Equal(expectedTenantId, currentTenant.TenantId);
            Assert.Equal(expectedTenantId.HasValue, currentTenant.IsResolved);
            return Task.CompletedTask;
        });

        await app.Build()(context);

        Assert.Equal(shouldCallNext, nextCalled);
        Assert.Same(currentTenant, scope.ServiceProvider.GetRequiredService<ICurrentTenant>());
        Assert.Equal(expectedTenantId, currentTenant.TenantId);
        Assert.Equal(expectedTenantId.HasValue, currentTenant.IsResolved);
        using var otherScope = provider.CreateScope();
        Assert.False(otherScope.ServiceProvider.GetRequiredService<CurrentTenant>().IsResolved);
        if (!shouldCallNext)
        {
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.StartsWith("application/problem+json", context.Response.ContentType);
            body.Position = 0;
            using var problem = await JsonDocument.ParseAsync(body);
            Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Tenant identifier is invalid.", problem.RootElement.GetProperty("title").GetString());
            Assert.Equal("X-Tenant-Id must contain exactly one non-empty GUID.",
                problem.RootElement.GetProperty("detail").GetString());
        }
    }
}
