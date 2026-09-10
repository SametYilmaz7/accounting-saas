using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SaaSPlatform.WebApi.Health;

namespace SaaSPlatform.UnitTests.Health;

public sealed class HealthEndpointTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, 200, "{\"status\":\"Healthy\"}")]
    [InlineData(HealthStatus.Unhealthy, 503, "{\"status\":\"Unhealthy\"}")]
    public async Task HealthEndpoint_ShouldReturnOnlyStatusWithoutTenantHeader(
        HealthStatus status, int expectedStatusCode, string expectedBody)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks().AddCheck("database", () => new HealthCheckResult(
            status, "Sensitive description", new Exception("Sensitive exception")));
        await using var app = builder.Build();
        app.MapPlatformHealth();
        var endpoint = Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints));
        Assert.Equal("/health", Assert.IsType<RouteEndpoint>(endpoint).RoutePattern.RawText);
        Assert.Equal(new[] { "GET" }, endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods);
        using var scope = app.Services.CreateScope();
        using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Response.Body = body;

        await endpoint.RequestDelegate!(context);

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        body.Position = 0;
        using var reader = new StreamReader(body);
        Assert.Equal(expectedBody, await reader.ReadToEndAsync());
    }
}
