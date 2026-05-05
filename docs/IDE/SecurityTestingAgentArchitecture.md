# Security Testing Agent - Architecture Documentation

## Overview

The Security Testing Agent provides professional pentesting capabilities for code security analysis. It performs vulnerability scanning, dependency analysis, and security best practices checks. Uses F# for vulnerability detection algorithms and security pattern analysis.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Vulnerability detection, security pattern analysis, risk scoring (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── SecurityTesting/
      ├── VulnerabilityType.cs            # Enum (SQLInjection, XSS, CSRF, etc.)
      /// SecurityVulnerability.cs        # Entity representing a security vulnerability
      /// SecurityTestResult.cs           # Value object for test result
      /// SecurityTestingAgent.cs         # AggregateRoot for security testing
      └── Events/
          ├── SecurityTestStartedEvent.cs
          ├── VulnerabilityFoundEvent.cs
          └── SecurityTestCompletedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── SecurityTestingAlgorithms.fs
      /// ScanForVulnerabilities          # Scan code for vulnerabilities
      /// AnalyzeDependencies            # Analyze dependencies for known CVEs
      /// CheckSecurityPatterns           # Check for security anti-patterns
      /// ScoreRisk                       # Score risk level
      └── RunSecurityTest                # Run full security test

Libr4.IDE.Application/
  └── SecurityTesting/
      ├── Commands/
          ├── RunSecurityTestCommand.cs
      ├── Queries/
          /// GetVulnerabilitiesQuery.cs
      ├── DTOs/
          /// SecurityVulnerabilityDto.cs
          /// SecurityTestResultDto.cs
          /// SecurityTestingAgentDto.cs
      ├── Handlers/
          ├── RunSecurityTestCommandHandler.cs
      └── Validators/
          └── RunSecurityTestCommandValidator.cs

Libr4.IDE.Api/
  └── SecurityTestingEndpoints.cs       # Minimal API endpoints
```

## Domain Model

### VulnerabilityType Enum

```csharp
public enum VulnerabilityType
{
    SQLInjection,
    XSS,
    CSRF,
    InsecureDeserialization,
    HardcodedSecrets,
    WeakCryptography,
    DependencyVulnerability
}
```

### SecurityVulnerability Entity

```csharp
public class SecurityVulnerability
{
    public Guid Id { get; }
    public string FilePath { get; }
    public int LineNumber { get; }
    public VulnerabilityType Type { get; }
    public string Description { get; }
    public Severity Severity { get; }
    public string Remediation { get; }
}
```

### SecurityTestResult Value Object

```csharp
public class SecurityTestResult
{
    public int TotalVulnerabilities { get; }
    public int CriticalCount { get; }
    public int HighCount { get; }
    public int MediumCount { get; }
    public int LowCount { get; }
    public double SecurityScore { get; }
}
```

### SecurityTestingAgent AggregateRoot

```csharp
public class SecurityTestingAgent : AggregateRoot<Guid>
{
    public string TestId { get; }
    public string WorkspaceId { get; }
    public List<SecurityVulnerability> Vulnerabilities { get; }
    public SecurityTestResult Result { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### ScanForVulnerabilities

Scans code for vulnerabilities:

```fsharp
let scanForVulnerabilities (files: (string * string) list) : SecurityVulnerability list =
    // Analyze each file
    // Detect vulnerability patterns
    // Return vulnerabilities
```

### AnalyzeDependencies

Analyzes dependencies for known CVEs:

```fsharp
let analyzeDependencies (dependencies: string list) : SecurityVulnerability list =
    // Check dependency versions
    // Look up known CVEs
    // Return dependency vulnerabilities
```

## Application Layer (C#)

### Command Handler

```csharp
public class RunSecurityTestCommandHandler : IRequestHandler<RunSecurityTestCommand, SecurityTestingAgentDto>
{
    private readonly ISecurityTestingAlgorithms _algorithms;
    
    public async Task<SecurityTestingAgentDto> Handle(RunSecurityTestCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var test = _algorithms.RunSecurityTest(
            request.WorkspaceId,
            request.Files,
            request.Dependencies
        );
        
        // Map to DTO
        return test.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class SecurityTestingEndpoints
{
    public static void MapSecurityTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/security-testing")
            .WithTags("Security Testing")
            .RequireAuthorization();
        
        group.MapPost("/test", async (
            RunSecurityTestCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunSecurityTest")
        .WithOpenApi();
    }
}
```

## Vulnerability Types

| Type | Description | Severity |
|------|-------------|----------|
| SQL Injection | SQL injection vulnerabilities | Critical |
| XSS | Cross-site scripting | High |
| CSRF | Cross-site request forgery | High |
| Insecure Deserialization | Unsafe deserialization | Critical |
| Hardcoded Secrets | Hardcoded passwords/keys | Critical |
| Weak Cryptography | Weak encryption algorithms | Medium |
| Dependency Vulnerability | Known CVEs in dependencies | Varies |

## Testing Strategy

1. **Unit Tests** - Test F# vulnerability detection algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full security testing flow

## Performance Considerations

- Security scanning can be CPU-intensive
- Use caching for dependency vulnerability checks
- Implement incremental scanning for large codebases

## Security Considerations

- Validate all file paths before scanning
- Sanitize code content
- Rate limit per user
- Audit all security test operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
