using SaaSPlatform.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTenantPersistence(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
