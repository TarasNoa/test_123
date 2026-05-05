# Hacker Agent - Architecture Documentation

## Overview

The Hacker Agent provides advanced security testing capabilities including script generation, GitHub integration for security tools, and comprehensive security testing. It can write custom security testing scripts, fetch tools from GitHub repositories, and execute full penetration testing workflows. Uses F# for script generation algorithms and GitHub integration logic.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation, GitHub client (Infrastructure)
- **F#** - Script generation, GitHub integration, security workflow orchestration (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── HackerAgent/
      ├── ScriptType.cs                  # Enum (Python, Bash, PowerShell, JavaScript)
      /// SecurityScript.cs               # Entity representing a security script
      /// GitHubSecurityTool.cs            # Entity representing a GitHub security tool
      /// HackerAgent.cs                   # AggregateRoot for hacker agent
      └── Events/
          ├── ScriptGeneratedEvent.cs
          ├── ToolFetchedEvent.cs
          └── SecurityTestCompletedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── HackerAgentAlgorithms.fs
      /// GenerateSecurityScript         # Generate security testing scripts
      /// FetchGitHubTools                # Fetch security tools from GitHub
      /// ExecuteSecurityWorkflow         # Execute full security testing workflow
      /// AnalyzeResults                  # Analyze security test results
      └── RunHackerAgent                  # Run full hacker agent operation

Libr4.IDE.Application/
  └── HackerAgent/
      ├── Commands/
          ├── RunHackerAgentCommand.cs
      ├── Queries/
          /// GetScriptsQuery.cs
      ├── DTOs/
          /// SecurityScriptDto.cs
          /// GitHubSecurityToolDto.cs
          /// HackerAgentDto.cs
      ├── Handlers/
          ├── RunHackerAgentCommandHandler.cs
      └── Validators/
          └── RunHackerAgentCommandValidator.cs

Libr4.IDE.Api/
  └── HackerAgentEndpoints.cs            # Minimal API endpoints
```

## Domain Model

### ScriptType Enum

```csharp
public enum ScriptType
{
    Python,
    Bash,
    PowerShell,
    JavaScript
}
```

### SecurityScript Entity

```csharp
public class SecurityScript
{
    public Guid Id { get; }
    public string ScriptName { get; }
    public ScriptType Type { get; }
    public string ScriptContent { get; }
    public string Description { get; }
    public bool IsCustom { get; }
}
```

### GitHubSecurityTool Entity

```csharp
public class GitHubSecurityTool
{
    public Guid Id { get; }
    public string RepoName { get; }
    public string RepoUrl { get; }
    public string Description { get; }
    public string ToolType { get; }
}
```

### HackerAgent AggregateRoot

```csharp
public class HackerAgent : AggregateRoot<Guid>
{
    public string OperationId { get; }
    public string WorkspaceId { get; }
    public List<SecurityScript> Scripts { get; }
    public List<GitHubSecurityTool> Tools { get; }
    public List<string> TestResults { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### GenerateSecurityScript

Generates security testing scripts:

```fsharp
let generateSecurityScript (scriptType: ScriptType) (target: string) : SecurityScript =
    // Generate script based on type and target
    // Return security script
```

### FetchGitHubTools

Fetches security tools from GitHub:

```fsharp
let fetchGitHubTools (query: string) : GitHubSecurityTool list =
    // Search GitHub for security tools
    // Return tools
```

## Application Layer (C#)

### Command Handler

```csharp
public class RunHackerAgentCommandHandler : IRequestHandler<RunHackerAgentCommand, HackerAgentDto>
{
    private readonly IHackerAgentAlgorithms _algorithms;
    
    public async Task<HackerAgentDto> Handle(RunHackerAgentCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var agent = _algorithms.RunHackerAgent(
            request.WorkspaceId,
            request.Target,
            request.ScriptType
        );
        
        // Map to DTO
        return agent.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class HackerAgentEndpoints
{
    public static void MapHackerAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/hacker-agent")
            .WithTags("Hacker Agent")
            .RequireAuthorization();
        
        group.MapPost("/run", async (
            RunHackerAgentCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunHackerAgent")
        .WithOpenApi();
    }
}
```

## Script Types

| Type | Extension | Use Case |
|------|-----------|----------|
| Python | .py | General security testing, API testing |
| Bash | .sh | Linux security testing, system commands |
| PowerShell | .ps1 | Windows security testing |
| JavaScript | .js | Web application security testing |

## GitHub Security Tools

| Tool | Repository | Type |
|------|-----------|------|
| OWASP ZAP | zaproxy/zaproxy | Web Scanner |
| Nmap | nmap/nmap | Network Scanner |
| Metasploit | rapid7/metasploit-framework | Exploitation Framework |
| Burp Suite | PortSwigger/burp-suite | Web Security Testing |

## Testing Strategy

1. **Unit Tests** - Test F# script generation algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full hacker agent workflow

## Performance Considerations

- Script generation is CPU-intensive
- GitHub API has rate limits
- Use caching for frequently used tools
- Implement background execution for long tests

## Security Considerations

- Validate all GitHub repositories before fetching
- Sanitize generated scripts
- Rate limit per user
- Audit all hacker agent operations
- Scripts should run in isolated environment

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
