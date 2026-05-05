/*
using Libr4.AI.Domain.Terminal;
using Libr4.IDE.Application.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Terminal sessions
/// Supports multiple terminal sessions with real-time output via WebSocket
/// </summary>
public static class TerminalEndpoints
{
    public static void MapTerminalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/terminal")
            .WithTags("Terminal")
            .RequireAuthorization()
            .WithOpenApi();

        // Create new terminal session
        group.MapPost("/sessions", async (
            [FromBody] CreateTerminalSessionRequest request,
            ITerminalService terminalService,
            CancellationToken ct) =>
        {
            var workspaceId = request.WorkspaceId ?? Guid.NewGuid().ToString();
            
            var session = await terminalService.CreateSessionAsync(
                workspaceId,
                request.Shell,
                request.WorkingDirectory,
                request.EnvironmentVariables,
                request.Rows ?? 24,
                request.Cols ?? 80,
                ct);

            return Results.Ok(session);
        })
        .WithName("CreateTerminalSession")
        .WithSummary("Create a new terminal session")
        .WithDescription("Creates a new terminal session for command execution in shadow workspace")
        .WithOpenApi();

        // List all sessions for user
        group.MapGet("/sessions", async (
            string? workspaceId,
            ITerminalService terminalService,
            CancellationToken ct) =>
        {
            var sessions = await terminalService.ListSessionsAsync(workspaceId, ct);
            return Results.Ok(sessions);
        })
        .WithName("ListTerminalSessions")
        .WithSummary("List terminal sessions")
        .WithDescription("Lists all terminal sessions for the current user")
        .WithOpenApi();

        // Get session by ID
        group.MapGet("/sessions/{sessionId}", async (
            string sessionId,
            ITerminalService terminalService,
            CancellationToken ct) =>
        {
            var session = await terminalService.GetSessionAsync(sessionId, ct);
            return session is not null ? Results.Ok(session) : Results.NotFound();
        })
        .WithName("GetTerminalSession")
        .WithSummary("Get terminal session details")
        .WithDescription("Retrieves details of a specific terminal session")
        .WithOpenApi();

        // Execute command in session
        group.MapPost("/execute", async (
            [FromBody] ExecuteCommandRequest request,
            ITerminalService terminalService,
            CancellationToken ct) =>
        {
            var entry = await terminalService.ExecuteCommandAsync(
                request.SessionId,
                request.Command,
                request.WorkingDirectory,
                ct);

            var session = await terminalService.GetSessionAsync(request.SessionId, ct);
            return Results.Ok(new { entry, session });
        })
        .WithName("ExecuteCommand")
        .WithSummary("Execute command in terminal session")
        .WithDescription("Executes a command in the specified terminal session and returns output")
        .WithOpenApi();

        // Get command history for session
        group.MapGet("/sessions/{sessionId}/history", async (
            string sessionId,
            ITerminalService terminalService,
            CancellationToken ct) =>
        {
            var history = await terminalService.GetHistoryAsync(sessionId, ct);
            return Results.Ok(history);
        })
        .WithName("GetTerminalHistory")
        .WithSummary("Get command history")
        .WithDescription("Retrieves command history for a terminal session")
        .WithOpenApi();

        // Terminate session
        group.MapPost("/sessions/{sessionId}/terminate", async (
            string sessionId,
            ITerminalService terminalService,
            CancellationToken ct) =>
        {
            await terminalService.TerminateSessionAsync(sessionId, ct);
            return Results.Ok();
        })
        .WithName("TerminateTerminalSession")
        .WithSummary("Terminate terminal session")
        .WithDescription("Terminates a terminal session and cleans up resources")
        .WithOpenApi();

        // Resize terminal
        group.MapPost("/sessions/{sessionId}/resize", async (
            string sessionId,
            [FromBody] ResizeTerminalRequest request,
            ITerminalService terminalService,
            CancellationToken ct) =>
        {
            await terminalService.ResizeAsync(sessionId, request.Rows, request.Cols, ct);
            return Results.Ok();
        })
        .WithName("ResizeTerminal")
        .WithSummary("Resize terminal")
        .WithDescription("Resizes the terminal session window")
        .WithOpenApi();
    }
}

// Request/Response DTOs
public record CreateTerminalSessionRequest
{
    public string? WorkspaceId { get; init; }
    public ShellType? Shell { get; init; }
    public string? WorkingDirectory { get; init; }
    public Dictionary<string, string>? EnvironmentVariables { get; init; }
    public int? Rows { get; init; }
    public int? Cols { get; init; }
}

public record ExecuteCommandRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string? WorkingDirectory { get; init; }
}

public record ResizeTerminalRequest
{
    public int Rows { get; init; }
    public int Cols { get; init; }
}
*/
