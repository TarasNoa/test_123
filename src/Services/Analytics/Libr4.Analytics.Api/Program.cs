using Libr4.Analytics.Api.Endpoints;
using Libr4.Analytics.Application;
using Libr4.Analytics.Application.Abstractions;
using Libr4.Analytics.Infrastructure;
using Libr4.Shared.Infrastructure;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Swagger;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog("analytics");
builder.Services.AddSharedInfrastructure(builder.Configuration);
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

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        if (app.Environment.IsDevelopment())
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }

app.Run();