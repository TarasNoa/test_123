using Microsoft.EntityFrameworkCore;
using Libr4.Shared.Infrastructure.Observability;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.CurrentUser;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Middleware;
using Libr4.Shared.Web.Swagger;
using Libr4.Trading.Api.Endpoints;
using Libr4.Trading.Application;
using Libr4.Trading.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddLibr4Serilog("trading");

// Application & Infrastructure
builder.Services.AddTradingApplication();
builder.Services.AddTradingInfrastructure(builder.Configuration);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddLibr4CurrentUser();
builder.Services.AddLibr4Telemetry("trading");
builder.Services.AddLibr4Swagger("ai");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapLibr4HealthChecks();
app.MapLibr4Metrics();

// REST Endpoints
app.MapOrderEndpoints();
app.MapMarketDataEndpoints();
app.MapPortfolioEndpoints();

// Ensure DB migrated on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Libr4.Trading.Infrastructure.Persistence.TradingDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
