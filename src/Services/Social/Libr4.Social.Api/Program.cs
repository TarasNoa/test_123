using Libr4.Social.Api.Endpoints;
using Libr4.Social.Application;
using Libr4.Social.Infrastructure;
using Libr4.Shared.Infrastructure;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Swagger;
using Libr4.Shared.Web.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Services.AddLibr4Logging();

// Auth
builder.Services.AddLibr4JwtAuth(builder.Configuration);

// Shared Infrastructure
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Social Application Layer
builder.Services.AddSocialApplication();

// Social Infrastructure Layer
builder.Services.AddSocialInfrastructure(builder.Configuration);

// API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLibr4Swagger("Social Service", "v1");

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://app.libr4.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapSocialNetworkEndpoints();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
        if (app.Environment.IsDevelopment())
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }

await app.RunAsync();