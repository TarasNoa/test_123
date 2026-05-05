using Microsoft.EntityFrameworkCore;
using Libr4.Chat.Api.Endpoints;
using Libr4.Chat.Infrastructure.Hubs;
using Libr4.Chat.Application;
using Libr4.Chat.Infrastructure;
using Libr4.Shared.Infrastructure.Observability;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.CurrentUser;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Middleware;
using Libr4.Shared.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.AddLibr4Serilog("chat");

// Application & Infrastructure
builder.Services.AddChatApplication();
builder.Services.AddChatInfrastructure(builder.Configuration);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddLibr4CurrentUser();
builder.Services.AddLibr4Telemetry("chat");
builder.Services.AddLibr4Swagger("chat");

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

// SignalR Hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationsHub>("/hubs/notifications");

// REST Endpoints
app.MapChatEndpoints();
app.MapMessageEndpoints();
app.MapNotificationEndpoints();
app.MapFileEndpoints();

app.Run();
