using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;

namespace Libr4.IDE.Domain.AI;

public class AITemplate : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string SystemPrompt { get; private set; }
    public string UserPromptTemplate { get; private set; }
    public List<string> RequiredVariables { get; private set; }
    public Dictionary<string, object> ExampleVariables { get; private set; }
    public string? RecommendedModel { get; private set; }
    public float? RecommendedTemperature { get; private set; }
    public int? RecommendedMaxTokens { get; private set; }
    public int UsageCount { get; private set; }
    public float SuccessRate { get; private set; }
    public bool IsPublic { get; private set; }
    public Guid? CreatorId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AITemplate() { }

    public static Result<AITemplate> Create(
        string name,
        string systemPrompt,
        string userPromptTemplate,
        string? description = null,
        string? category = null,
        List<string>? requiredVariables = null,
        Dictionary<string, object>? exampleVariables = null,
        string? recommendedModel = null,
        float? recommendedTemperature = null,
        int? recommendedMaxTokens = null,
        bool isPublic = false,
        Guid? creatorId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<AITemplate>(Error.Validation("Template.Name.Required", "Template name is required"));

        if (name.Length > 200)
            return Result.Failure<AITemplate>(Error.Validation("Template.Name.TooLong", "Template name cannot exceed 200 characters"));

        if (string.IsNullOrWhiteSpace(systemPrompt))
            return Result.Failure<AITemplate>(Error.Validation("Template.SystemPrompt.Required", "System prompt is required"));

        if (string.IsNullOrWhiteSpace(userPromptTemplate))
            return Result.Failure<AITemplate>(Error.Validation("Template.UserPromptTemplate.Required", "User prompt template is required"));

        var template = new AITemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Category = category,
            SystemPrompt = systemPrompt,
            UserPromptTemplate = userPromptTemplate,
            RequiredVariables = requiredVariables ?? new List<string>(),
            ExampleVariables = exampleVariables ?? new Dictionary<string, object>(),
            RecommendedModel = recommendedModel,
            RecommendedTemperature = recommendedTemperature,
            RecommendedMaxTokens = recommendedMaxTokens,
            UsageCount = 0,
            SuccessRate = 0.0f,
            IsPublic = isPublic,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        template.RaiseDomainEvent(new AITemplateCreatedEvent(template.Id, name, isPublic));
        return Result.Success(template);
    }

    public Result Update(
        string? name = null,
        string? description = null,
        string? category = null,
        string? systemPrompt = null,
        string? userPromptTemplate = null)
    {
        if (name != null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure(Error.Validation("Template.Name.Required", "Template name is required"));
            if (name.Length > 200)
                return Result.Failure(Error.Validation("Template.Name.TooLong", "Template name cannot exceed 200 characters"));
            Name = name;
        }

        if (description != null) Description = description;
        if (category != null) Category = category;
        if (systemPrompt != null && !string.IsNullOrWhiteSpace(systemPrompt)) SystemPrompt = systemPrompt;
        if (userPromptTemplate != null && !string.IsNullOrWhiteSpace(userPromptTemplate)) UserPromptTemplate = userPromptTemplate;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RecordUsage(bool wasSuccessful)
    {
        UsageCount++;
        if (wasSuccessful)
        {
            SuccessRate = (SuccessRate * (UsageCount - 1) + 1.0f) / UsageCount;
        }
        else
        {
            SuccessRate = (SuccessRate * (UsageCount - 1)) / UsageCount;
        }
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}

public record AITemplateCreatedEvent(Guid TemplateId, string Name, bool IsPublic) : DomainEvent;
