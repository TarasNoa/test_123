using Microsoft.AspNetCore.Mvc;
using Libr4.Collaboration.Application;
using Libr4.Collaboration.Application.Abstractions;
using Libr4.Collaboration.Application.Commands;
using Libr4.Collaboration.Application.Queries;
using Libr4.Collaboration.Domain;
using Libr4.Collaboration.Infrastructure.Persistence;
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
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddLibr4Serilog("collaboration");
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddCollaborationApplication();

builder.Services.AddDbContext<CollaborationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "collaboration")));

builder.Services.AddScoped<ICollaborationRoomRepository, EfCollaborationRoomRepository>();
builder.Services.AddScoped<IDocumentRepository, EfDocumentRepository>();
builder.Services.AddScoped<IWhiteboardRepository, EfWhiteboardRepository>();
builder.Services.AddScoped<IVideoCallRepository, EfVideoCallRepository>();

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

// Ensure database is created for E2E testing
using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CollaborationDbContext>();
        if (app.Environment.IsDevelopment())
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }

app.Run();
