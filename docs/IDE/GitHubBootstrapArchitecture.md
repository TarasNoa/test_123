# GitHub Template Bootstrap Service - Architecture Documentation

## Overview

The GitHub Template Bootstrap Service searches GitHub for suitable project templates, checks licenses, and seeds new projects with template code. Uses F# for repository search algorithms and license validation logic.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Repository search, license validation, template matching (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── GitHubBootstrap/
      ├── LicenseType.cs                 # Enum (MIT, Apache, GPL, BSD, None)
      ├── GitHubRepo.cs                   # Entity representing a GitHub repository
      ├── TemplateMatch.cs                # Value object for template match
      ├── BootstrapProject.cs             # AggregateRoot for bootstrap project
      └── Events/
          ├── RepoSearchedEvent.cs
          ├── LicenseCheckedEvent.cs
          └── ProjectSeededEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── GitHubBootstrapAlgorithms.fs
      ├── SearchRepositories             # Search GitHub repositories
      /// ValidateLicense                # Validate repository license
      /// MatchTemplate                  # Match repository to requirements
      /// SeedProject                    # Seed project with template
      └── BootstrapProject              # Bootstrap new project

Libr4.IDE.Application/
  └── GitHubBootstrap/
      ├── Commands/
          ├── BootstrapProjectCommand.cs
      ├── Queries/
          ├── SearchTemplatesQuery.cs
      ├── DTOs/
          ├── GitHubRepoDto.cs
          /// TemplateMatchDto.cs
          /// BootstrapProjectDto.cs
      ├── Handlers/
          ├── BootstrapProjectCommandHandler.cs
          /// SearchTemplatesQueryHandler.cs
      └── Validators/
          └── BootstrapProjectCommandValidator.cs

Libr4.IDE.Api/
  └── GitHubBootstrapEndpoints.cs        # Minimal API endpoints
```

## Domain Model

### LicenseType Enum

```csharp
public enum LicenseType
{
    MIT,
    Apache,
    GPL,
    BSD,
    None
}
```

### GitHubRepo Entity

```csharp
public class GitHubRepo
{
    public Guid Id { get; }
    public string RepoName { get; }
    public string Owner { get; }
    public string Description { get; }
    public LicenseType License { get; }
    public int Stars { get; }
    public string Url { get; }
}
```

### TemplateMatch Value Object

```csharp
public class TemplateMatch
{
    public GitHubRepo Repository { get; }
    public double MatchScore { get; }
    public string MatchReason { get; }
}
```

### BootstrapProject AggregateRoot

```csharp
public class BootstrapProject : AggregateRoot<Guid>
{
    public string ProjectId { get; }
    public string ProjectName { get; }
    public GitHubRepo SelectedTemplate { get; }
    public List<string> FilesCreated { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### SearchRepositories

Searches GitHub repositories:

```fsharp
let searchRepositories (query: string) (language: string) : GitHubRepo list =
    // Search GitHub API
    // Filter by language and stars
    // Return repositories
```

### ValidateLicense

Validates repository license:

```fsharp
let validateLicense (repo: GitHubRepo) (allowedLicenses: LicenseType list) : bool =
    // Check if license is allowed
    // Return validation result
```

## Application Layer (C#)

### Command Handler

```csharp
public class BootstrapProjectCommandHandler : IRequestHandler<BootstrapProjectCommand, BootstrapProjectDto>
{
    private readonly IGitHubBootstrapAlgorithms _algorithms;
    
    public async Task<BootstrapProjectDto> Handle(BootstrapProjectCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var project = _algorithms.BootstrapProject(
            request.ProjectName,
            request.Language,
            request.AllowedLicenses
        );
        
        // Map to DTO
        return project.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class GitHubBootstrapEndpoints
{
    public static void MapGitHubBootstrapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/github-bootstrap")
            .WithTags("GitHub Bootstrap")
            .RequireAuthorization();
        
        group.MapPost("/bootstrap", async (
            BootstrapProjectCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BootstrapProject")
        .WithOpenApi();
    }
}
```

## License Types

| License | Permissive | Commercial Use |
|---------|------------|----------------|
| MIT | Yes | Yes |
| Apache | Yes | Yes |
| GPL | No | No |
| BSD | Yes | Yes |
| None | N/A | No |

## Testing Strategy

1. **Unit Tests** - Test F# search and validation algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full bootstrap flow

## Performance Considerations

- GitHub API has rate limits
- Use caching for repository searches
- Implement background seeding for large projects

## Security Considerations

- Validate all GitHub URLs
- Sanitize repository names
- Rate limit per user
- Audit all bootstrap operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
