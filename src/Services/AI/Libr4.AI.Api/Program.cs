using Microsoft.EntityFrameworkCore;
using Libr4.AI.Api.Endpoints;
using Libr4.AI.Application;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure;
using Libr4.AI.Infrastructure.LLM;
using Libr4.AI.Infrastructure.Hooks;
using Libr4.AI.Infrastructure.Hooks.BuiltIn;
using Libr4.Shared.Infrastructure.Observability;
using Libr4.Shared.Web.Auth;
using Libr4.Shared.Web.CurrentUser;
using Libr4.Shared.Web.HealthChecks;
using Libr4.Shared.Web.Logging;
using Libr4.Shared.Web.Middleware;
using Libr4.Shared.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.AddLibr4Serilog("ai");

// Application & Infrastructure
builder.Services.AddAIApplication();
builder.Services.AddAIInfrastructure(builder.Configuration);

builder.Services.AddLibr4JwtAuth(builder.Configuration);
builder.Services.AddLibr4CurrentUser();
builder.Services.AddLibr4Telemetry("ai");
builder.Services.AddLibr4Swagger("ai");

var app = builder.Build();

// Initialize Hook System
var hookManager = app.Services.GetRequiredService<HookManager>();
// var sessionLoggingHook = app.Services.GetRequiredService<SessionLoggingHook>();
var toolUsageLoggingHook = app.Services.GetRequiredService<ToolUsageLoggingHook>();
var contextCompressionHook = app.Services.GetRequiredService<ContextCompressionHook>();
var humanizerHook = app.Services.GetRequiredService<HumanizerHook>();

// hookManager.RegisterHook(sessionLoggingHook);
hookManager.RegisterHook(toolUsageLoggingHook);
hookManager.RegisterHook(contextCompressionHook);
hookManager.RegisterHook(humanizerHook);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapLibr4HealthChecks();
app.MapLibr4Metrics();

// REST Endpoints
app.MapChatEndpoints();
app.MapAgentEndpoints();
app.MapTranslationEndpoints();
app.MapCodeGraphEndpoints();
app.MapSubagentEndpoints();
app.MapRouterEndpoints();
app.MapExecutorEndpoints();
app.MapOrchestrationEndpoints();
app.MapMultiProviderEndpoints();
app.MapReactionEndpoints();
app.MapVoiceEndpoints();

// OpenAI-compatible endpoint
app.MapPost("/v1/chat/completions", async (
    ChatCompletionRequest request,
    ILLMProviderFactory factory,
    CancellationToken ct) =>
{
    var provider = factory.GetProvider(request.Model);
    var result = await provider.CompleteAsync(new(
        request.Model,
        request.Messages.Select(m => new Libr4.AI.Application.Abstractions.ChatMessage(m.Role, m.Content)).ToList(),
        request.Temperature ?? 0.7f,
        request.MaxTokens ?? 2000), ct);

    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
}).RequireAuthorization().WithTags("OpenAI Compatible");

// Ensure DB migrated on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Libr4.AI.Infrastructure.Persistence.AIDbContext>();
    await db.Database.MigrateAsync();
    
    // Initialize session logging store if needed
    // var sessionLogDb = scope.ServiceProvider.GetService<Libr4.AI.Infrastructure.SessionLogging.SessionLogDbContext>();
    // if (sessionLogDb != null)
    // {
    //     await sessionLogDb.Database.MigrateAsync();
    // }
}

app.Run();

// OpenAI-compatible request
public record ChatCompletionRequest(
    string Model,
    List<Message> Messages,
    float? Temperature = null,
    int? MaxTokens = null);

public record Message(string Role, string Content);
