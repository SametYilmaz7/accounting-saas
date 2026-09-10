using SaaSPlatform.WebApi;
using SaaSPlatform.WebApi.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTenantPersistence(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    // Bootstrap tenant resolution only; the header does not establish authorization.
    app.UseMiddleware<TenantResolutionMiddleware>();
}

app.Run();
