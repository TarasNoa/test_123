using Microsoft.AspNetCore.Mvc;
using Libr4.Collaboration.Application;
using Libr4.Collaboration.Application.Abstractions;
using Libr4.Collaboration.Application.Commands;
using Libr4.Collaboration.Application.Queries;
using Libr4.Collaboration.Domain;
using Libr4.Collaboration.Infrastructure.Repositories;
using MediatR;
using Libr4.Collaboration.Api.Endpoints;
using Libr4.Collaboration.Infrastructure.Hubs;
using Libr4.Shared.Infrastructure;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Swagger;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLibr4Logging();
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddCollaborationApplication();

builder.Services.AddScoped<ICollaborationRoomRepository, InMemoryCollaborationRoomRepository>();
builder.Services.AddScoped<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddScoped<IWhiteboardRepository, InMemoryWhiteboardRepository>();
builder.Services.AddScoped<IVideoCallRepository, InMemoryVideoCallRepository>();

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("collaboration", opt =>
    {
        opt.PermitLimit = 200;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

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
app.MapCollaborationEndpoints();
app.MapHub<CollaborationHub>("/collaborationHub");

app.Run();
