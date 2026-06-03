using Microsoft.EntityFrameworkCore;
using Libr4.Chat.Api.Endpoints;
using Libr4.Chat.Api.Hubs;
using Libr4.Chat.Application;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Infrastructure;
using Libr4.Chat.Infrastructure.Persistence;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.CurrentUser;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Persistence;
using Libr4.Shared.Web.Swagger;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Cors;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog("chat");
builder.Services.AddChatApplication();
builder.Services.AddChatInfrastructure(builder.Configuration);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddLibr4CurrentUser();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://libr4-frontend.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("chat", opt =>
    {
        opt.PermitLimit = 150;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 20;
    });
});

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapChatEndpoints();
app.MapServerEndpoints();
app.MapCodeShareEndpoints();
app.MapMessageEndpoints();
app.MapNotificationEndpoints();
app.MapFileEndpoints();
app.MapCallEndpoints();
app.MapHub<ChatHub>("/chatHub").RequireAuthorization();

await app.ApplyDatabaseBootstrapAsync<ChatDbContext>(
    useMigrations: !app.Environment.IsDevelopment());

app.Run();
