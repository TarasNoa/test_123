# Autonomous Runtime Policy Service - Architecture Documentation

## Overview

The Autonomous Runtime Policy Service defines policies for autonomous runtime, including domain signals (regulated, safety-critical), runtime evidence signals, and rich app build signals. It infers domain class and requirements for runtime proof and rich app builds, which are crucial for quality contracts and approval workflows.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Policy inference algorithms, domain classification, quality contract generation (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── AutonomousRuntimePolicy/
      ├── DomainSignal.cs              # Enum (None, Regulated, SafetyCritical)
      ├── RuntimeEvidenceSignal.cs      # Enum (None, ApprovalRequired, AuditTrailRequired)
      ├── QualityContract.cs           # Value object for quality contract
      ├── RuntimePolicy.cs             # AggregateRoot for runtime policy
      └── Events/
          ├── PolicyGeneratedEvent.cs
          └── QualityContractRequiredEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── AutonomousRuntimePolicyAlgorithms.fs
      ├── InferDomainClass            # Infer domain class from prompt
      ├── InferRuntimeProofRequired    # Infer runtime proof requirements
      ├── InferRichAppBuildRequired    # Infer rich app build requirements
      ├── GenerateQualityContract      # Generate quality contract
      ├── CheckApprovalCheckpoint     # Check if approval checkpoint is needed
      └── ValidatePolicyCompliance    # Validate policy compliance

Libr4.IDE.Application/
  └── AutonomousRuntimePolicy/
      ├── Commands/
          ├── GeneratePolicyCommand.cs
      ├── Queries/
          ├── GetPolicyQuery.cs
      ├── DTOs/
          ├── QualityContractDto.cs
          ├── RuntimePolicyDto.cs
      ├── Handlers/
          ├── GeneratePolicyCommandHandler.cs
          ├── GetPolicyQueryHandler.cs
      └── Validators/
          └── GeneratePolicyCommandValidator.cs

Libr4.IDE.Api/
  └── AutonomousRuntimePolicyEndpoints.cs     # Minimal API endpoints
```

## Domain Model

### DomainSignal Enum

```csharp
public enum DomainSignal
{
    None,
    Regulated,      // Banking/fintech, healthcare
    SafetyCritical  // Spacecraft, medical devices, industrial control
}
```

### RuntimeEvidenceSignal Enum

```csharp
public enum RuntimeEvidenceSignal
{
    None,
    ApprovalRequired,
    AuditTrailRequired,
    BothRequired
}
```

### QualityContract Value Object

```csharp
public class QualityContract
{
    public bool ApprovalRequired { get; }
    public bool AuditTrailRequired { get; }
    public List<string> QualityChecks { get; }
    public Dictionary<string, object> QualityThresholds { get; }
    public string ApprovalWorkflow { get; }
}
```

### RuntimePolicy AggregateRoot

```csharp
public class RuntimePolicy : AggregateRoot<Guid>
{
    public string PolicyId { get; }
    public string Prompt { get; }
    public DomainSignal DomainSignal { get; }
    public RuntimeEvidenceSignal RuntimeEvidenceSignal { get; }
    public bool RuntimeProofRequired { get; }
    public bool RichAppBuildRequired { get; }
    public QualityContract QualityContract { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### InferDomainClass

Infers domain class from prompt:

```fsharp
let inferDomainClass (prompt: string) : DomainSignal =
    let lower = prompt.ToLower()
    let regulatedKeywords = ["banking"; "fintech"; "finance"; "money"; "payment"; "health"; "medical"]
    let safetyKeywords = ["spacecraft"; "aerospace"; "industrial"; "control"; "safety-critical"; "medical device"]
    
    if regulatedKeywords |> List.exists (fun kw -> lower.Contains(kw)) then
        DomainSignal.Regulated
    elif safetyKeywords |> List.exists (fun kw -> lower.Contains(kw)) then
        DomainSignal.SafetyCritical
    else
        DomainSignal.None
```

### InferRuntimeProofRequired

Infers runtime proof requirements:

```fsharp
let inferRuntimeProofRequired (domainSignal: DomainSignal) (prompt: string) : bool =
    match domainSignal with
    | DomainSignal.Regulated -> true
    | DomainSignal.SafetyCritical -> true
    | DomainSignal.None ->
        let lower = prompt.ToLower()
        lower.Contains("approval") || lower.Contains("audit")
```

### InferRichAppBuildRequired

Infers rich app build requirements:

```fsharp
let inferRichAppBuildRequired (prompt: string) : bool =
    let lower = prompt.ToLower()
    let richKeywords = ["production"; "deploy"; "release"; "shippable"; "complete"]
    richKeywords |> List.exists (fun kw -> lower.Contains(kw))
```

### GenerateQualityContract

Generates quality contract based on domain:

```fsharp
let generateQualityContract (domainSignal: DomainSignal) : QualityContract =
    match domainSignal with
    | DomainSignal.Regulated ->
        QualityContract.Create(
            true,
            true,
            ["security_scan"; "compliance_check"; "audit_trail"],
            dict[("min_coverage", 0.95)],
            "regulatory_approval"
        )
    | DomainSignal.SafetyCritical ->
        QualityContract.Create(
            true,
            true,
            ["safety_check"; "failure_mode_analysis"; "audit_trail"],
            dict[("min_coverage", 0.99)],
            "safety_approval"
        )
    | DomainSignal.None ->
        QualityContract.Create(
            false,
            false,
            ["basic_testing"],
            dict[("min_coverage", 0.8)],
            "standard_review"
        )
```

## Application Layer (C#)

### Command Handler

```csharp
public class GeneratePolicyCommandHandler : IRequestHandler<GeneratePolicyCommand, RuntimePolicyDto>
{
    private readonly IAutonomousRuntimePolicyAlgorithms _algorithms;
    
    public async Task<RuntimePolicyDto> Handle(GeneratePolicyCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var runtimePolicy = _algorithms.GeneratePolicy(
            request.Prompt,
            request.WorkspaceId
        );
        
        // Map to DTO
        return runtimePolicy.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class AutonomousRuntimePolicyEndpoints
{
    public static void MapAutonomousRuntimePolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/autonomous-policy")
            .WithTags("Autonomous Runtime Policy")
            .RequireAuthorization();
        
        group.MapPost("/generate-policy", async (
            GeneratePolicyCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("GenerateRuntimePolicy")
        .WithOpenApi();
    }
}
```

## Domain Signals

| Domain Signal | Description | Keywords | Requirements |
|---------------|-------------|-----------|--------------|
| None | Standard production SaaS | - | Basic quality checks |
| Regulated | Banking/fintech, healthcare | banking, fintech, finance, money, payment, health, medical | Approval required, audit trail required |
| SafetyCritical | Spacecraft, medical devices, industrial control | spacecraft, aerospace, industrial, control, safety-critical | Approval required, audit trail required, failure mode analysis |

## Quality Contracts

| Domain | Approval Required | Audit Trail Required | Quality Checks | Min Coverage | Approval Workflow |
|--------|------------------|---------------------|----------------|--------------|-------------------|
| Regulated | Yes | Yes | Security scan, compliance check, audit trail | 95% | Regulatory approval |
| SafetyCritical | Yes | Yes | Safety check, failure mode analysis, audit trail | 99% | Safety approval |
| None | No | No | Basic testing | 80% | Standard review |

## Testing Strategy

1. **Unit Tests** - Test F# policy inference algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full policy generation flow

## Performance Considerations

- Policy inference is CPU-light (keyword matching)
- Use caching for similar policy requests
- Implement rate limiting on API endpoints

## Security Considerations

- Validate all policy parameters
- Sanitize prompt before keyword analysis
- Rate limit per user
- Audit all policy generations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
