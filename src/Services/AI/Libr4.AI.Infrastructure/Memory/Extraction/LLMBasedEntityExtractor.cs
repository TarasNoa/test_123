/*
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Application.Memory;

namespace Libr4.AI.Infrastructure.Memory.Extraction;

/// <summary>
/// LLM-based entity extractor using structured output.
/// Supports extraction of people, organizations, concepts, and relationships.
/// </summary>
public sealed class LLMBasedEntityExtractor : IEntityExtractor
{
    private readonly ILLMProviderFactory _llmFactory;
    private readonly ILogger<LLMBasedEntityExtractor> _logger;

    public LLMBasedEntityExtractor(
        ILLMProviderFactory llmFactory,
        ILogger<LLMBasedEntityExtractor> logger)
    {
        _llmFactory = llmFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExtractedEntity>> ExtractEntitiesAsync(
        string text,
        ExtractionOptions? options = null,
        CancellationToken ct = default)
    {
        var result = await ExtractWithRelationshipsAsync(text, options, ct);
        return result.Entities;
    }

    public async Task<ExtractionResult> ExtractWithRelationshipsAsync(
        string text,
        ExtractionOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new ExtractionOptions();
        
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ExtractionResult(
                Entities: Array.Empty<ExtractedEntity>(),
                Relationships: Array.Empty<EntityRelationship>());
        }

        var provider = _llmFactory.GetDefaultProvider();
        
        var systemPrompt = @"You are an expert entity extraction system. 
Analyze the provided text and extract:
1. Named entities (people, organizations, locations, products, concepts)
2. Relationships between these entities

Return ONLY a valid JSON object with no markdown formatting.";

        var userPrompt = $@"Extract entities and relationships from this text:

---
{text}
---

Return JSON in this exact format:
{{
  ""entities"": [
    {{
      ""id"": ""unique_id"",
      ""name"": ""Entity Name"",
      ""type"": ""Person|Organization|Location|Product|Concept|Technology|Event"",
      ""description"": ""Brief description"",
      ""confidence"": 0.95,
      ""mentions"": [
        {{
          ""startIndex"": 0,
          ""endIndex"": 10,
          ""context"": ""surrounding text""
        }}
      ]
    }}
  ],
  ""relationships"": [
    {{
      ""sourceEntityId"": ""id1"",
      ""targetEntityId"": ""id2"",
      ""relationshipType"": ""works_for|located_in|created_by|part_of|related_to"",
      ""confidence"": 0.9
    }}
  ]
}}

Entity Types:
- Person: Individual people, roles, titles
- Organization: Companies, teams, departments, institutions
- Location: Places, addresses, regions
- Product: Software, tools, services
- Concept: Ideas, methodologies, principles
- Technology: Programming languages, frameworks, protocols
- Event: Meetings, releases, milestones

Relationship Types:
- works_for: Employment relationship
- located_in: Geographic containment
- created_by: Authorship/creator relationship  
- part_of: Hierarchical containment
- related_to: General association
- uses: Technology/tool usage
- mentions: Simple reference

Be precise and only extract clearly stated entities.";

        var completionRequest = new CompletionRequest(
            Model: options.PreferredModel ?? "gpt-4o-mini",
            Messages: new List<ChatMessage>
            {
                new("system", systemPrompt),
                new("user", userPrompt)
            },
            Temperature: 0.1f, // Low temperature for consistency
            MaxTokens: 2000);

        try
        {
            var result = await provider.CompleteAsync(completionRequest, ct);
            
            if (!result.IsSuccess)
            {
                _logger.LogError("LLM entity extraction failed: {Error}", result.Error);
                return new ExtractionResult(
                    Entities: Array.Empty<ExtractedEntity>(),
                    Relationships: Array.Empty<EntityRelationship>());
            }

            var response = result.Value;
            var jsonContent = ExtractJsonFromResponse(response.Content);
            
            var extractionResult = JsonSerializer.Deserialize<EntityExtractionResult>(jsonContent);
            
            if (extractionResult?.Entities == null)
            {
                _logger.LogWarning("Failed to parse entity extraction result");
                return new ExtractionResult(
                    Entities: Array.Empty<ExtractedEntity>(),
                    Relationships: Array.Empty<EntityRelationship>());
            }

            // Filter by confidence
            var filteredEntities = extractionResult.Entities
                .Where(e => e.Confidence >= options.MinConfidence)
                .Select(e => new ExtractedEntity(
                    Id: e.Id ?? Guid.NewGuid().ToString(),
                    Name: e.Name,
                    Type: NormalizeEntityType(e.Type),
                    Description: e.Description,
                    Confidence: e.Confidence,
                    Mentions: e.Mentions?.Select(m => new EntityMention(
                        m.StartIndex,
                        m.EndIndex,
                        m.Context)).ToList() ?? new List<EntityMention>()))
                .ToList();

            var relationships = extractionResult.Relationships
                ?.Where(r => r.Confidence >= options.MinConfidence)
                ?.Select(r => new EntityRelationship(
                    SourceEntityId: r.SourceEntityId,
                    TargetEntityId: r.TargetEntityId,
                    RelationshipType: NormalizeRelationshipType(r.RelationshipType),
                    Confidence: r.Confidence))
                ?.ToList() ?? new List<EntityRelationship>();

            _logger.LogDebug(
                "Extracted {EntityCount} entities and {RelCount} relationships from text",
                filteredEntities.Count, relationships.Count);

            return new ExtractionResult(filteredEntities, relationships);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entity extraction failed");
            return new ExtractionResult(
                Entities: Array.Empty<ExtractedEntity>(),
                Relationships: Array.Empty<EntityRelationship>());
        }
    }

    public async Task<IReadOnlyList<EntityLinkCandidate>> LinkEntitiesAsync(
        IReadOnlyList<ExtractedEntity> entities,
        string userId,
        CancellationToken ct = default)
    {
        // This would query the graph database for existing entities
        // and calculate similarity scores
        // For now, return simple string matching
        
        var candidates = new List<EntityLinkCandidate>();
        
        foreach (var entity in entities)
        {
            // In production, this would query Neo4j for similar entities:
            // MATCH (e:Entity) WHERE e.name CONTAINS $namePart OR e.aliases CONTAINS $name
            // RETURN e, similarity
            
            // Simplified: assume no existing link for now
            candidates.Add(new EntityLinkCandidate(
                NewEntity: entity,
                ExistingEntityId: null,
                ExistingEntityName: null,
                SimilarityScore: 0,
                ShouldLink: false));
        }

        return candidates;
    }

    private static string ExtractJsonFromResponse(string response)
    {
        // Try to extract JSON from markdown code blocks
        if (response.Contains("```json"))
        {
            var start = response.IndexOf("```json") + 7;
            var end = response.IndexOf("```", start);
            if (end > start)
            {
                return response.Substring(start, end - start).Trim();
            }
        }
        
        // Try to extract from generic code blocks
        if (response.Contains("```"))
        {
            var start = response.IndexOf("```") + 3;
            var end = response.IndexOf("```", start);
            if (end > start)
            {
                var content = response.Substring(start, end - start).Trim();
                if (content.StartsWith("{"))
                    return content;
            }
        }
        
        // Find JSON object directly
        var jsonStart = response.IndexOf("{");
        var jsonEnd = response.LastIndexOf("}");
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }
        
        return response;
    }

    private static string NormalizeEntityType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return "Concept";
        
        var normalized = type.Trim().ToLowerInvariant();
        return normalized switch
        {
            "person" or "people" or "individual" => "Person",
            "organization" or "org" or "company" or "institution" => "Organization",
            "location" or "place" or "address" or "region" => "Location",
            "product" or "tool" or "software" or "service" => "Product",
            "concept" or "idea" or "methodology" or "principle" => "Concept",
            "technology" or "framework" or "language" or "protocol" => "Technology",
            "event" or "meeting" or "release" or "milestone" => "Event",
            _ => "Concept"
        };
    }

    private static string NormalizeRelationshipType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return "related_to";
        
        var normalized = type.Trim().ToLowerInvariant().Replace(" ", "_");
        return normalized switch
        {
            "works_for" or "employed_by" or "member_of" => "works_for",
            "located_in" or "in" or "at" => "located_in",
            "created_by" or "author" or "developer" => "created_by",
            "part_of" or "belongs_to" or "component_of" => "part_of",
            "uses" or "utilizes" or "implements" => "uses",
            "mentions" or "references" or "cites" => "mentions",
            _ => "related_to"
        };
    }

    // JSON models for LLM response
    private sealed class EntityExtractionResult
    {
        [JsonPropertyName("entities")]
        public List<ExtractedEntityDto> Entities { get; set; } = new();
        
        [JsonPropertyName("relationships")]
        public List<EntityRelationshipDto> Relationships { get; set; } = new();
    }

    private sealed class ExtractedEntityDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }
        
        [JsonPropertyName("mentions")]
        public List<EntityMentionDto> Mentions { get; set; } = new();
    }

    private sealed class EntityMentionDto
    {
        [JsonPropertyName("startIndex")]
        public int StartIndex { get; set; }
        
        [JsonPropertyName("endIndex")]
        public int EndIndex { get; set; }
        
        [JsonPropertyName("context")]
        public string Context { get; set; } = "";
    }

    private sealed class EntityRelationshipDto
    {
        [JsonPropertyName("sourceEntityId")]
        public string SourceEntityId { get; set; } = "";
        
        [JsonPropertyName("targetEntityId")]
        public string TargetEntityId { get; set; } = "";
        
        [JsonPropertyName("relationshipType")]
        public string RelationshipType { get; set; } = "";
        
        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }
    }
}
*/
