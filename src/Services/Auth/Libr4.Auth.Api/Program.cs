using Libr4.Auth.Api.Configuration;
using Libr4.Auth.Api.Endpoints;
using Libr4.Auth.Application;
using Libr4.Auth.Infrastructure;
using Libr4.Auth.Infrastructure.Persistence;
using Libr4.Shared.Infrastructure.Observability;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.CurrentUser;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Middleware;
using Libr4.Shared.Web.Swagger;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog("auth");

builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);

// Validate JWT configuration (throws in production if using dev keys)
JwtConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddLibr4CurrentUser();
builder.Services.AddLibr4Telemetry("auth");
builder.Services.AddLibr4Swagger("Libr4 Auth API");

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

var app = builder.Build();

// Auto-migrate database on startup
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapLibr4HealthChecks();
app.MapLibr4Metrics();
app.MapAuthEndpoints();
app.MapSession1Endpoints();

app.Run();

public partial class Program;
