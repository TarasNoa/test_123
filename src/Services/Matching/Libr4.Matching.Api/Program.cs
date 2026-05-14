using Libr4.Matching.Application;
using Libr4.Matching.Infrastructure;
using Libr4.Matching.Api.Endpoints;
using Libr4.Matching.Infrastructure.Persistence;
using Libr4.Shared.Web.Auth;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMatchingApplication();
builder.Services.AddMatchingInfrastructure(builder.Configuration);
builder.Services.AddLibr4JwtAuth(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapMatchingEndpoints();
app.MapHealthChecks("/health");

// Ensure database is created for E2E testing
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MatchingDbContext>();
    try { db.Database.Migrate(); } catch { db.Database.EnsureCreated(); }
}

app.Run();
