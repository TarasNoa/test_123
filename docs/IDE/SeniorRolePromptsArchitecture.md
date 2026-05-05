# IDE Agent Senior Role Prompts Service - Architecture Documentation

## Overview

The Senior Role Prompts Service provides phase-specific role instructions for IDE agents. Each execution phase gets a tailored prompt with senior-level expectations (Technical Program Lead, Staff Backend Engineer, Staff Frontend Engineer, etc.), ensuring the AI behaves like a coordinated team of experts rather than a shallow generalist.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Prompt generation algorithms, pattern matching (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── SeniorRolePrompts/
      ├── DomainClass.cs              # Enum (Standard, Regulated, SafetyCritical)
      ├── PhaseType.cs                # Enum (Planning, Database, BackendAPI, Frontend, etc.)
      ├── SeniorRole.cs               # Value object representing a senior role
      ├── RolePrompt.cs               # Domain entity
      └── Events/
          └── PromptGeneratedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── SeniorRolePromptsAlgorithms.fs
      ├── GetSeniorRoleForPhase       # Map phase to senior role
      ├── BuildPhaseSystemPrompt      # Build system prompt for phase
      ├── BuildRoleBrief              # Build user prompt role brief
      ├── GetProductionTeamCharter    # Base team standards
      └── GetDomainHint               # Domain-specific guidance

Libr4.IDE.Application/
  └── SeniorRolePrompts/
      ├── Commands/
          ├── GenerateRolePromptCommand.cs
      ├── Queries/
          ├── GetRolePromptQuery.cs
      ├── DTOs/
          ├── RolePromptDto.cs
          ├── PhasePromptDto.cs
      ├── Handlers/
          ├── GenerateRolePromptCommandHandler.cs
          ├── GetRolePromptQueryHandler.cs
      └── Validators/
          └── GenerateRolePromptCommandValidator.cs

Libr4.IDE.Api/
  └── SeniorRolePromptsEndpoints.cs   # Minimal API endpoints
```

## Domain Model

### DomainClass Enum

```csharp
public enum DomainClass
{
    Standard,           // Production SaaS
    Regulated,          // Banking/fintech, healthcare
    SafetyCritical      // Spacecraft, medical devices, industrial control
}
```

### PhaseType Enum

```csharp
public enum PhaseType
{
    Planning,
    DatabaseModel,
    BackendAPI,
    BackendService,
    FrontendExperience,
    Enrichment,
    UXPolish,
    BackendRuntime,
    FrontendRuntime,
    BackendQuality,
    FrontendQuality,
    BrowserE2E
}
```

### SeniorRole Value Object

```csharp
public class SeniorRole
{
    public string RoleTitle { get; }
    public string Description { get; }
    public List<string> Responsibilities { get; }
    public List<string> Capabilities { get; }
    public string ReviewProfile { get; }
}
```

### RolePrompt Entity

```csharp
public class RolePrompt
{
    public Guid Id { get; }
    public PhaseType PhaseType { get; }
    public string PhaseName { get; }
    public SeniorRole SeniorRole { get; }
    public string SystemPrompt { get; }
    public string UserPrompt { get; }
    public DomainClass DomainClass { get; }
    public bool RichMode { get; }
}
```

## F# Algorithms

### GetSeniorRoleForPhase

Maps each phase to a specific senior role:

```fsharp
let getSeniorRoleForPhase (phaseType: PhaseType) (domainClass: string) : SeniorRole =
    match phaseType with
    | PhaseType.Planning ->
        { RoleTitle = "Technical Program Lead + Staff Architect"
          Description = "Produce an implicit architecture that later phases can implement"
          Responsibilities = ["Architecture"; "Non-functional requirements"; "Security"; "Observability"] }
    | PhaseType.DatabaseModel ->
        { RoleTitle = "Staff Backend Engineer (data layer)"
          Description = "Own the database models, relationships, and migrations" }
    | PhaseType.BackendAPI ->
        { RoleTitle = "Staff Backend Engineer (API + domain services)"
          Description = "Routers stay thin; services own transactions, validation, and business rules" }
    | PhaseType.FrontendExperience ->
        { RoleTitle = "Staff Frontend Engineer + Product Designer"
          Description = "Ship real navigable flows with loading/error/empty states" }
    // ... more phases
```

### BuildPhaseSystemPrompt

Builds the system prompt for a phase:

```fsharp
let buildPhaseSystemPrompt (phaseName: string) (domainClass: string) (richMode: bool) : string =
    let roleLine = 
        match domainClass with
        | "regulated" -> "Domain posture: regulated fintech — safety, auditability, and truthful money logic are mandatory."
        | "safety_critical" -> "Domain posture: safety-critical control plane — approvals, audit trails, and explicit failure modes are mandatory."
        | _ -> "Domain posture: production SaaS — reliability, clarity, and maintainability over demos."
    
    let rich = 
        if richMode then
            "This run is a rich multi-phase product build: each FILE block must be shippable-quality for that slice, not a sketch."
        else
            "Deliver complete files for the phase scope; avoid partial modules."
    
    sprintf "Current phase title: %s. You embody the senior specialist described in the user message.\n\n%s\n\n%s\n\nOutput ONLY FILE/TYPE/CONTENT/EXPLANATION blocks; no markdown fences around the whole answer."
        phaseName roleLine rich
```

### GetProductionTeamCharter

Base team standards that apply to all phases:

```fsharp
let getProductionTeamCharter () : string =
    """You are part of a senior product-engineering team shipping a real, reviewable codebase — not a demo.

Team standards (every phase):
- Cross-phase continuity: when the user prompt lists files from earlier phases, treat them as the source of truth
- Completeness over breadth: fewer files that compile, parse, and wire together beat many half-finished files
- No placeholder UI copy, TODO/FIXME in runtime paths, fake auth, or "in production we would…" deferrals
- Consistent module graph: every import must resolve to a file you emit in this project or that already exists
- FastAPI/SQLAlchemy wiring: use app.db.base for Base, app.db.session for get_db/engine/session
- Regulated / money-moving domains: fail closed; explicit auth, ownership, limits, and audit signals in code
- Close every string, bracket, JSX tag before ending a file; never truncate mid-docstring"""
```

## Application Layer (C#)

### Command Handler

```csharp
public class GenerateRolePromptCommandHandler : IRequestHandler<GenerateRolePromptCommand, RolePromptDto>
{
    private readonly ISeniorRolePromptsAlgorithms _algorithms;
    
    public async Task<RolePromptDto> Handle(GenerateRolePromptCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var rolePrompt = _algorithms.GenerateRolePrompt(
            request.PhaseType,
            request.DomainClass,
            request.RichMode
        );
        
        // Map to DTO
        return rolePrompt.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class SeniorRolePromptsEndpoints
{
    public static void MapSeniorRolePromptsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/senior-prompts")
            .WithTags("Senior Role Prompts")
            .RequireAuthorization();
        
        group.MapPost("/generate", async (
            GenerateRolePromptCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("GenerateRolePrompt")
        .WithOpenApi();
        
        group.MapGet("/phases/{phaseType}", async (
            string phaseType,
            string domainClass = "standard",
            bool richMode = false,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetRolePromptQuery(phaseType, domainClass, richMode);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetRolePrompt")
        .WithOpenApi();
    }
}
```

## Phase-to-Role Mapping

| Phase Type | Senior Role | Key Responsibilities |
|-------------|-------------|-------------------|
| Planning | Technical Program Lead + Staff Architect | Architecture, non-functional requirements, security, observability |
| DatabaseModel | Staff Backend Engineer (data layer) | Models, relationships, migrations, database consistency |
| BackendAPI | Staff Backend Engineer (API + domain services) | Thin routers, service transactions, validation, business rules |
| BackendService | Staff Backend Engineer (domain services) | Service layer, business logic, domain modeling |
| FrontendExperience | Staff Frontend Engineer + Product Designer | Navigable flows, loading/error states, visual hierarchy |
| Enrichment | Principal Engineer (security + domain depth) | Negative paths, idempotency, rate limiting, audit logging |
| UXPolish | Senior Product Designer + Frontend craft | Microcopy, motion, responsive layouts, accessibility |
| BackendRuntime | Senior DevOps / Platform (backend) | Docker, healthchecks, README, requirements.txt |
| FrontendRuntime | Senior DevOps (frontend) + DX | package.json scripts, Docker multi-stage, .env.example |
| BackendQuality | Senior QA Automation (backend) | Tests, import paths, happy path + business denial |
| FrontendQuality | Senior QA Automation (frontend) | Component tests, mock network, split large files |
| BrowserE2E | Senior SDET (browser) | Playwright specs, page objects, negative flows |

## Domain-Specific Guidance

### Regulated Domain (Banking/Fintech)

```
Domain posture: regulated fintech — safety, auditability, and truthful money logic are mandatory.

Banking/fintech: balances, transfers, cards, beneficiaries, risk/audit must be modeled honestly with rejection paths.
```

### Safety-Critical Domain (Spacecraft/Medical)

```
Domain posture: safety-critical control plane — approvals, audit trails, and explicit failure modes are mandatory.

Mission-critical: telemetry, commands, approvals, anomalies, audit — no toy dashboards without real state transitions.
```

### Standard Domain (Production SaaS)

```
Domain posture: production SaaS — reliability, clarity, and maintainability over demos.
```

## Testing Strategy

1. **Unit Tests** - Test F# prompt generation algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full flow from API to prompt generation

## Performance Considerations

- Prompt generation is CPU-light (string manipulation)
- Use caching for frequently used phase/domain combinations
- Implement rate limiting on API endpoints

## Security Considerations

- Validate all phase types and domain classes
- Sanitize phase names to prevent prompt injection
- Rate limit per user
- Audit all prompt generations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
