using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog("gateway");

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// Rate limiting for preview routes
// builder.Services.AddPreviewRateLimiting(); // TODO: Uncomment when available

// Circuit breaker for Shadow Workspace preview resilience
// builder.Services.AddCircuitBreaker(); // TODO: Uncomment when available

// Dynamic preview router for Shadow Workspace
// builder.Services.AddSingleton<DynamicPreviewRouter>(); // TODO: Uncomment when available
// builder.Services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<DynamicPreviewRouter>()); // TODO: Uncomment when available
// builder.Services.AddHostedService<PreviewCleanupBackgroundService>(); // TODO: Uncomment when available

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
// app.UseRateLimiter();  // TODO: Uncomment when RateLimitingExtensions is available
app.UseAuthorization();

app.MapLibr4HealthChecks();

// Preview management endpoints
// app.MapPreviewManagementEndpoints(); // TODO: Uncomment when available

app.MapReverseProxy();

app.Run();
