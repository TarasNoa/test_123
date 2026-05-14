using Libr4.Auth.Api.Configuration;
using Libr4.Auth.Api.Endpoints;
using Libr4.Auth.Application;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Infrastructure;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Swagger;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Libr4.Auth.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog("auth");

builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);

// Validate JWT configuration (throws in production if using dev keys)
JwtConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate Limiting for auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapSession1Endpoints();

// Ensure database is created for E2E testing
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    try { db.Database.Migrate(); } catch { db.Database.EnsureCreated(); }
}

app.Run();

public partial class Program;
