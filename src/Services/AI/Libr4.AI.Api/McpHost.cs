/*
using Libr4.AI.Infrastructure.MCP;
using Libr4.AI.Application.Memory;
using System.Text.Json;

namespace Libr4.AI.Api;

/// <summary>
/// Host for Libr4 MCP Server with all tools registered.
/// </summary>
public sealed class Libr4McpHost : IHostedService
{
    private readonly IMcpServer _mcpServer;
    private readonly IHybridMemoryService _memory;
    private readonly ISubagentRegistry _subagents;
    private readonly IContextCompressionService _compression;
    private readonly IAgentsMdParser _agentsMd;
    private readonly ILogger<Libr4McpHost> _logger;

    public Libr4McpHost(
        IHybridMemoryService memory,
        ISubagentRegistry subagents,
        IContextCompressionService compression,
        IAgentsMdParser agentsMd,
        ILogger<Libr4McpHost> logger)
    {
        _memory = memory;
        _subagents = subagents;
        _compression = compression;
        _agentsMd = agentsMd;
        _logger = logger;
        
        var options = new McpServerOptions
        {
            Name = "libr4",
            Version = "2.0.0",
            Transport = McpTransport.Stdio // Can be changed to HttpSse
        };
        
        _mcpServer = new McpServer(options);
    }

    public Task StartAsync(CancellationToken ct)
    {
        RegisterTools();
        RegisterResources();
        RegisterPrompts();
        
        _logger.LogInformation("Libr4 MCP Server starting...");
        return _mcpServer.RunAsync(ct);
    }

    public Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Libr4 MCP Server stopping...");
        return Task.CompletedTask;
    }

    private void RegisterTools()
    {
        // Memory tools
        _mcpServer.RegisterTool(new McpTool
        {
            Name = "remember",
            Description = "Store information in memory with optional tags",
            InputSchema = JsonSerializer.Deserialize<JsonElement>("""
            {
                "type": "object",
                "properties": {
                    "content": { "type": "string", "description": "Content to remember" },
                    "level": { "type": "string", "enum": ["user", "session", "agent"], "description": "Memory level" },
                    "tags": { "type": "array", "items": { "type": "string" }, "description": "Optional tags" }
                },
                "required": ["content"]
            }
            """),
            Handler = async (args, ct) =>
            {
                var content = args.GetProperty("content").GetString()!;
                var levelStr = args.TryGetProperty("level", out var l) ? l.GetString() : "user";
                var level = Enum.Parse<MemoryLevel>(levelStr!, true);
                
                var result = await _memory.RememberAsync(content, level, ct: ct);
                return $"Stored memory with ID: {result.Id}";
            }
        });

        _mcpServer.RegisterTool(new McpTool
        {
            Name = "recall",
            Description = "Recall memories based on semantic query",
            InputSchema = JsonSerializer.Deserialize<JsonElement>("""
            {
                "type": "object",
                "properties": {
                    "query": { "type": "string", "description": "Search query" },
                    "limit": { "type": "integer", "default": 5, "description": "Max results" }
                },
                "required": ["query"]
            }
            """),
            Handler = async (args, ct) =>
            {
                var query = args.GetProperty("query").GetString()!;
                var limit = args.TryGetProperty("limit", out var lim) ? lim.GetInt32() : 5;
                
                var results = await _memory.RecallAsync(query, new RecallOptions { TopK = limit }, ct);
                return string.Join("\n", results.Select(r => $"- {r.Content} (score: {r.CombinedScore:F2})"));
            }
        });

        _mcpServer.RegisterTool(new McpTool
        {
            Name = "list_agents",
            Description = "List available agents/subagents",
            InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                {"type": "object", "properties": {}}
            """),
            Handler = async (args, ct) =>
            {
                var agents = _subagents.GetAllSubagents();
                return string.Join("\n", agents.Select(a => $"- {a.Name}: {a.Description}"));
            }
        });

        _mcpServer.RegisterTool(new McpTool
        {
            Name = "compress_context",
            Description = "Compress text to fit token budget",
            InputSchema = JsonSerializer.Deserialize<JsonElement>("""
            {
                "type": "object",
                "properties": {
                    "text": { "type": "string", "description": "Text to compress" },
                    "max_tokens": { "type": "integer", "default": 4000, "description": "Target token count" }
                },
                "required": ["text"]
            }
            """),
            Handler = async (args, ct) =>
            {
                var text = args.GetProperty("text").GetString()!;
                var maxTokens = args.TryGetProperty("max_tokens", out var mt) ? mt.GetInt32() : 4000;
                
                var items = new[] { new ContextItem { Content = text, Type = "input" } };
                var result = await _compression.CompressAsync(
                    items, 
                    new CompressionOptions { TargetTokens = maxTokens },
                    ct);
                
                return result.Items.FirstOrDefault()?.Content ?? text;
            }
        });

        _mcpServer.RegisterTool(new McpTool
        {
            Name = "parse_agents_md",
            Description = "Parse AGENTS.md file in current project",
            InputSchema = JsonSerializer.Deserialize<JsonElement>("""
            {
                "type": "object",
                "properties": {
                    "path": { "type": "string", "description": "Path to AGENTS.md file (optional, finds nearest if not specified)" }
                }
            }
            """),
            Handler = async (args, ct) =>
            {
                string? path = null;
                if (args.TryGetProperty("path", out var p))
                    path = p.GetString();
                
                AgentsMdDocument? doc;
                if (path != null)
                    doc = await _agentsMd.ParseFileAsync(path, ct);
                else
                    doc = await _agentsMd.FindNearestAsync(Directory.GetCurrentDirectory(), ct);
                
                if (doc == null)
                    return "No AGENTS.md found";
                
                return doc.ToContextPrompt();
            }
        });
    }

    private void RegisterResources()
    {
        _mcpServer.RegisterResource(new McpResource
        {
            Uri = "memory://stats",
            Name = "Memory Statistics",
            Description = "Current memory system statistics",
            MimeType = "application/json",
            Handler = async (ct) =>
            {
                var stats = await _memory.GetStatisticsAsync(ct);
                return JsonSerializer.Serialize(new
                {
                    stats.VectorRecords,
                    stats.GraphNodes,
                    stats.GraphRelationships
                });
            }
        });

        _mcpServer.RegisterResource(new McpResource
        {
            Uri = "agents://list",
            Name = "Agent List",
            Description = "List of all available agents",
            MimeType = "application/json",
            Handler = async (ct) =>
            {
                var agents = _subagents.GetAllSubagents();
                return JsonSerializer.Serialize(agents.Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Description,
                    a.Capabilities
                }));
            }
        });
    }

    private void RegisterPrompts()
    {
        _mcpServer.RegisterPrompt(new McpPrompt
        {
            Name = "code_review",
            Description = "Review code with context from memory",
            Arguments = new List<McpPromptArgument>
            {
                new() { Name = "code", Description = "Code to review", Required = true },
                new() { Name = "language", Description = "Programming language", Required = false }
            },
            Handler = async (args, ct) =>
            {
                var code = args.GetValueOrDefault("code", "");
                var lang = args.GetValueOrDefault("language", "unknown");
                
                // Recall relevant patterns from memory
                var patterns = await _memory.RecallAsync(
                    $"code review patterns {lang}",
                    new RecallOptions { TopK = 3 },
                    ct);
                
                var memoryContext = string.Join("\n", patterns.Select(p => p.Content));
                
                return new List<McpPromptMessage>
                {
                    new()
                    {
                        Role = "system",
                        Content = new McpTextContent
                        {
                            Type = "text",
                            Text = $"You are a code reviewer. Consider these patterns:\n{memoryContext}"
                        }
                    },
                    new()
                    {
                        Role = "user",
                        Content = new McpTextContent
                        {
                            Type = "text",
                            Text = $"Review this {lang} code:\n```\n{code}\n```"
                        }
                    }
                };
            }
        });

        _mcpServer.RegisterPrompt(new McpPrompt
        {
            Name = "architecture_decision",
            Description = "Make architecture decision with project context",
            Arguments = new List<McpPromptArgument>
            {
                new() { Name = "decision", Description = "What needs to be decided", Required = true }
            },
            Handler = async (args, ct) =>
            {
                var decision = args.GetValueOrDefault("decision", "");
                
                // Get project context from AGENTS.md
                var agentsMd = await _agentsMd.FindNearestAsync(Directory.GetCurrentDirectory(), ct);
                var context = agentsMd?.ToContextPrompt(2000) ?? "No project context available.";
                
                return new List<McpPromptMessage>
                {
                    new()
                    {
                        Role = "system",
                        Content = new McpTextContent
                        {
                            Type = "text",
                            Text = $"You are an architect. Consider this project context:\n{context}"
                        }
                    },
                    new()
                    {
                        Role = "user",
                        Content = new McpTextContent
                        {
                            Type = "text",
                            Text = $"Help me decide: {decision}"
                        }
                    }
                };
            }
        });
    }
}

// Interface shims for DI
public interface ISubagentRegistry
{
    IReadOnlyList<AgentInfo> GetAllSubagents();
}

public interface IAgentsMdParser
{
    Task<AgentsMdDocument> ParseFileAsync(string path, CancellationToken ct);
    Task<AgentsMdDocument?> FindNearestAsync(string startPath, CancellationToken ct);
}

public class AgentInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Capabilities { get; set; } = new();
}

public class AgentsMdDocument
{
    public string ToContextPrompt(int maxLength = 4000) => "";
}
*/
