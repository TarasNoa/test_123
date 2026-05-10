using Libr4.Analytics.Api.Endpoints;
using Libr4.Analytics.Application;
using Libr4.Analytics.Application.Abstractions;
using Libr4.Analytics.Infrastructure;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLibr4Logging();
builder.Services.AddAnalyticsApplication();
builder.Services.AddAnalyticsInfrastructure(builder.Configuration);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapMetricsEndpoints();
app.MapDashboardEndpoints();

app.Run();