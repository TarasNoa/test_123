/*
using MediatR;
using Libr4.IDE.Application.SemanticCodeGraph.Commands;
using Libr4.IDE.Application.SemanticCodeGraph.DTOs;
using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.SemanticCodeGraph.Handlers;

/// <summary>
/// Handler for BuildGraphCommand - AI-powered semantic code graph builder
/// </summary>
public class BuildGraphCommandHandler : IRequestHandler<BuildGraphCommand, SemanticGraphDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<BuildGraphCommandHandler> _logger;

    public BuildGraphCommandHandler(IAIService aiService, ILogger<BuildGraphCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<SemanticGraphDto> Handle(BuildGraphCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Building semantic code graph for {Count} files", request.Files.Count);
        
        var nodes = new List<CodeNodeDto>();
        var edges = new List<CodeEdgeDto>();
        
        foreach (var (filePath, content) in request.Files)
        {
            var fileNodes = ExtractNodes(filePath, content);
            nodes.AddRange(fileNodes);
        }
        
        // Build relationships between nodes
        edges = BuildEdges(nodes);
        
        // AI-enhanced semantic analysis
        var semanticClusters = await BuildSemanticClustersAsync(nodes, ct);
        
        return new SemanticGraphDto
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Nodes = nodes,
            Edges = edges,
            Clusters = semanticClusters,
            CreatedAt = DateTime.UtcNow
        };
    }

    private List<CodeNodeDto> ExtractNodes(string filePath, string content)
    {
        var nodes = new List<CodeNodeDto>();
        var lines = content.Split('\n');
        
        // Extract classes
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Class detection
            var classMatch = Regex.Match(line, @"class\s+(\w+)");
            if (classMatch.Success)
            {
                nodes.Add(new CodeNodeDto
                {
                    Id = Guid.NewGuid(),
                    Name = classMatch.Groups[1].Value,
                    Type = "Class",
                    FilePath = filePath,
                    LineNumber = i + 1,
                    CodeSnippet = line.Trim()
                });
            }
            
            // Method detection
            var methodMatch = Regex.Match(line, @"(public|private|protected)\s+\w+\s+(\w+)\s*\(");
            if (methodMatch.Success)
            {
                nodes.Add(new CodeNodeDto
                {
                    Id = Guid.NewGuid(),
                    Name = methodMatch.Groups[2].Value,
                    Type = "Method",
                    FilePath = filePath,
                    LineNumber = i + 1,
                    CodeSnippet = line.Trim()
                });
            }
            
            // Interface detection
            var interfaceMatch = Regex.Match(line, @"interface\s+(\w+)");
            if (interfaceMatch.Success)
            {
                nodes.Add(new CodeNodeDto
                {
                    Id = Guid.NewGuid(),
                    Name = interfaceMatch.Groups[1].Value,
                    Type = "Interface",
                    FilePath = filePath,
                    LineNumber = i + 1,
                    CodeSnippet = line.Trim()
                });
            }
        }
        
        return nodes;
    }

    private List<CodeEdgeDto> BuildEdges(List<CodeNodeDto> nodes)
    {
        var edges = new List<CodeEdgeDto>();
        
        // Simple edge detection: same file = related
        var fileGroups = nodes.GroupBy(n => n.FilePath);
        foreach (var group in fileGroups)
        {
            var fileNodes = group.ToList();
            for (int i = 0; i < fileNodes.Count; i++)
            {
                for (int j = i + 1; j < fileNodes.Count; j++)
                {
                    if (Math.Abs(fileNodes[i].LineNumber - fileNodes[j].LineNumber) < 50)
                    {
                        edges.Add(new CodeEdgeDto
                        {
                            Id = Guid.NewGuid(),
                            SourceId = fileNodes[i].Id,
                            TargetId = fileNodes[j].Id,
                            Relationship = "proximity",
                            Weight = 0.5
                        });
                    }
                }
            }
        }
        
        return edges;
    }

    private async Task<List<SemanticClusterDto>> BuildSemanticClustersAsync(
        List<CodeNodeDto> nodes, 
        CancellationToken ct)
    {
        var clusters = new List<SemanticClusterDto>();
        
        try
        {
            // Group by type for initial clusters
            var typeGroups = nodes.GroupBy(n => n.Type);
            foreach (var group in typeGroups)
            {
                var nodeNames = group.Select(n => n.Name).Take(10).ToList();
                
                var prompt = $"""
                    Analyze these {group.Key}s: {string.Join(", ", nodeNames)}
                    
                    Suggest 2-3 semantic clusters/categories for grouping.
                    Return format: CategoryName|description
                    """;
                
                var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);
                
                var lines = response.Split('\n');
                foreach (var line in lines.Where(l => l.Contains('|')))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 1)
                    {
                        clusters.Add(new SemanticClusterDto
                        {
                            Id = Guid.NewGuid(),
                            Name = parts[0].Trim(),
                            Description = parts.Length > 1 ? parts[1].Trim() : "",
                            NodeIds = group.Select(n => n.Id).ToList()
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI clustering failed");
        }
        
        return clusters;
    }
}
*/
