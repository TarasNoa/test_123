using Microsoft.EntityFrameworkCore;
using Libr4.AI.Api.Endpoints;
using Libr4.AI.Application;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure;
using Libr4.AI.Infrastructure.LLM;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Swagger;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLibr4Logging();
builder.Services.AddAIApplication();
builder.Services.AddAIInfrastructure(builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

// Authentication & Authorization
builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://libr4-frontend.com", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

var app = builder.Build();

// Middleware
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Metrics
app.MapPrometheusScrapingEndpoint();

// API Endpoints
app.MapAgentEndpoints();
app.MapOrderAssistantEndpoints();
app.MapTaskRecommendationEndpoints();
app.MapTranslationEndpoints();
app.MapVoiceEndpoints();

// Global Error Handler
app.UseExceptionHandler("/error");
app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500));

app.Run();
