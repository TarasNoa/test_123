using Libr4.Shared.Infrastructure.Observability;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.CurrentUser;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Middleware;
using Libr4.Shared.Web.Swagger;
using Libr4.Tasks.Api.Endpoints;
using Libr4.Tasks.Application;
using Libr4.Tasks.Infrastructure;
using Libr4.Tasks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddLibr4Serilog("tasks");

builder.Services.AddTasksApplication();
builder.Services.AddTasksInfrastructure(builder.Configuration);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddLibr4CurrentUser();
builder.Services.AddLibr4Telemetry("tasks");
builder.Services.AddLibr4Swagger("Libr4 Tasks API");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapLibr4HealthChecks();
app.MapLibr4Metrics();

app.MapTaskEndpoints();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TasksDbContext>();
    try { db.Database.Migrate(); } catch { db.Database.EnsureCreated(); }
}

app.Run();
