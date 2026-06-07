using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public sealed record ResumeSeedSnapshot(
    Guid SourceRunId,
    GenerationPlan Plan,
    IReadOnlyList<GeneratedFile> Files,
    string UserRequest);

/// <summary>
/// Loads a prior run exported via GET /api/ide/app-generation/{id} for fix-only resume after host restart.
/// </summary>
public static class ResumeSeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ResumeSeedSnapshot? TryLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        var report = JsonSerializer.Deserialize<AppGenerationReportDto>(json, JsonOptions);
        if (report?.Plan is null || report.Files.Count == 0)
            return null;

        var plan = ToDomainPlan(report.Plan);
        var files = report.Files
            .Select(f => new GeneratedFile(f.RelativePath, f.Language, f.Content))
            .ToList();

        const string defaultRequest =
            "Mobile banking (transfers, accounts). Backend Java Spring Boot in backend/. Frontend React TypeScript in frontend/.";

        return new ResumeSeedSnapshot(report.Id, plan, files, defaultRequest);
    }

    private static GenerationPlan ToDomainPlan(GenerationPlanDto dto)
    {
        var phases = dto.Phases
            .Select(p => new GenerationPhase(
                p.Order,
                p.Name,
                p.Description,
                p.Assignments
                    .Select(a => new AgentAssignment(a.AgentName, a.Role, a.TaskDescription))
                    .ToList()))
            .ToList();

        var tech = new TechStack(
            dto.TechStack.Languages.ToList(),
            dto.TechStack.Frameworks.ToList(),
            dto.TechStack.Databases.ToList(),
            dto.TechStack.Infrastructure.ToList(),
            dto.TechStack.Rationale);

        return new GenerationPlan(
            dto.ApplicationName,
            dto.ApplicationDescription,
            tech,
            phases,
            dto.RequiredAgents.ToList(),
            dto.RuntimeImage,
            dto.BuildCommands.ToList(),
            dto.TestCommands.ToList(),
            dto.MaxIterations);
    }
}
