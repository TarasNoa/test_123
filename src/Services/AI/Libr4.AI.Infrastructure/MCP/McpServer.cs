/*
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Libr4.AI.Infrastructure.MCP;

/// <summary>
/// C# native implementation of Model Context Protocol (MCP) server.
/// Supports stdio and HTTP/SSE transports.
/// </summary>
public interface IMcpServer
{
    Task RunAsync(CancellationToken ct = default);
    void RegisterTool(McpTool tool);
    void RegisterResource(McpResource resource);
    void RegisterPrompt(McpPrompt prompt);
}

/// <summary>
/// Main MCP Server implementation with full protocol support.
/// </summary>
public sealed class McpServer : IMcpServer, IDisposable
{
    private readonly McpServerOptions _options;
    private readonly List<McpTool> _tools = new();
    private readonly List<McpResource> _resources = new();
    private readonly List<McpPrompt> _prompts = new();
    private readonly ILogger<McpServer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServer(McpServerOptions options, ILogger<McpServer>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<McpServer>.Instance;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    public void RegisterTool(McpTool tool)
    {
        _tools.Add(tool);
        _logger.LogInformation("Registered MCP tool: {ToolName}", tool.Name);
    }

    public void RegisterResource(McpResource resource)
    {
        _resources.Add(resource);
        _logger.LogInformation("Registered MCP resource: {ResourceUri}", resource.Uri);
    }

    public void RegisterPrompt(McpPrompt prompt)
    {
        _prompts.Add(prompt);
        _logger.LogInformation("Registered MCP prompt: {PromptName}", prompt.Name);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting MCP Server v{Version}", _options.Version);

        switch (_options.Transport)
        {
            case McpTransport.Stdio:
                await RunStdioAsync(ct);
                break;
            case McpTransport.HttpSse:
                await RunHttpSseAsync(ct);
                break;
            default:
                throw new NotSupportedException($"Transport {_options.Transport} not supported");
        }
    }

    private async Task RunStdioAsync(CancellationToken ct)
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin, Encoding.UTF8);
        using var writer = new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true };

        _logger.LogInformation("MCP Server listening on stdio");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                _logger.LogDebug("Received: {Line}", line[..Math.Min(line.Length, 200)]);

                var request = JsonSerializer.Deserialize<McpJsonRpcRequest>(line, _jsonOptions);
                if (request == null) continue;

                var response = await HandleRequestAsync(request, ct);
                
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await writer.WriteLineAsync(responseJson.ToCharArray(), ct);
                
                _logger.LogDebug("Sent: {Response}", responseJson[..Math.Min(responseJson.Length, 200)]);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling request");
            }
        }
    }

    private async Task RunHttpSseAsync(CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(_options.HttpUrl ?? "http://localhost:5007");
        
        var app = builder.Build();

        // SSE endpoint for server-to-client messages
        app.MapGet("/sse", async (HttpContext ctx) =>
        {
            ctx.Response.Headers.Append("Content-Type", "text/event-stream");
            ctx.Response.Headers.Append("Cache-Control", "no-cache");
            ctx.Response.Headers.Append("Connection", "keep-alive");

            var sessionId = Guid.NewGuid().ToString();
            
            // Send endpoint event
            await ctx.Response.WriteAsync($"event: endpoint\ndata: /message?sessionId={sessionId}\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);

            // Keep connection open
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(30000, ct); // Heartbeat every 30s
                await ctx.Response.WriteAsync($": heartbeat\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        });

        // POST endpoint for client-to-server messages
        app.MapPost("/message", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync(ct);
            
            var request = JsonSerializer.Deserialize<McpJsonRpcRequest>(body, _jsonOptions);
            if (request == null)
            {
                ctx.Response.StatusCode = 400;
                return;
            }

            var response = await HandleRequestAsync(request, ct);
            
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(response, _jsonOptions, ct);
        });

        _logger.LogInformation("MCP Server listening on {Url}", _options.HttpUrl);
        await app.RunAsync(ct);
    }

    private async Task<McpJsonRpcResponse> HandleRequestAsync(McpJsonRpcRequest request, CancellationToken ct)
    {
        try
        {
            object? result = request.Method switch
            {
                "initialize" => HandleInitialize(request.Params),
                "initialized" => null, // Notification, no response needed
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolCallAsync(request.Params, ct),
                "resources/list" => HandleResourcesList(),
                "resources/read" => await HandleResourceReadAsync(request.Params, ct),
                "prompts/list" => HandlePromptsList(),
                "prompts/get" => await HandlePromptGetAsync(request.Params, ct),
                _ => throw new McpMethodNotFoundException($"Method not found: {request.Method}")
            };

            return new McpJsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Result = result != null ? JsonSerializer.SerializeToElement(result, _jsonOptions) : null
            };
        }
        catch (McpException ex)
        {
            return new McpJsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError
                {
                    Code = ex.Code,
                    Message = ex.Message
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling method {Method}", request.Method);
            return new McpJsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                }
            };
        }
    }

    private McpInitializeResult HandleInitialize(JsonElement? @params)
    {
        return new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            ServerInfo = new McpServerInfo
            {
                Name = _options.Name,
                Version = _options.Version
            },
            Capabilities = new McpServerCapabilities
            {
                Tools = _tools.Any() ? new { } : null,
                Resources = _resources.Any() ? new { } : null,
                Prompts = _prompts.Any() ? new { } : null
            }
        };
    }

    private McpToolsListResult HandleToolsList()
    {
        return new McpToolsListResult
        {
            Tools = _tools.Select(t => new McpToolSchema
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = t.InputSchema
            }).ToList()
        };
    }

    private async Task<McpToolCallResult> HandleToolCallAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null) throw new McpInvalidParamsException("Missing params");

        var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(@params.Value, _jsonOptions);
        if (paramsDict == null || !paramsDict.TryGetValue("name", out var nameElement))
        {
            throw new McpInvalidParamsException("Missing tool name");
        }

        var toolName = nameElement.GetString();
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        
        if (tool == null)
        {
            throw new McpInvalidParamsException($"Tool not found: {toolName}");
        }

        var arguments = paramsDict.TryGetValue("arguments", out var argsElement) 
            ? argsElement 
            : new JsonElement();

        _logger.LogInformation("Calling tool: {ToolName}", toolName);

        var result = await tool.Handler(arguments, ct);

        return new McpToolCallResult
        {
            Content = new List<McpContent>
            {
                new McpTextContent { Type = "text", Text = result }
            },
            IsError = false
        };
    }

    private McpResourcesListResult HandleResourcesList()
    {
        return new McpResourcesListResult
        {
            Resources = _resources.Select(r => new McpResourceSchema
            {
                Uri = r.Uri,
                Name = r.Name,
                MimeType = r.MimeType,
                Description = r.Description
            }).ToList()
        };
    }

    private async Task<McpResourceReadResult> HandleResourceReadAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null) throw new McpInvalidParamsException("Missing params");

        var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(@params.Value, _jsonOptions);
        if (paramsDict == null || !paramsDict.TryGetValue("uri", out var uriElement))
        {
            throw new McpInvalidParamsException("Missing resource URI");
        }

        var uri = uriElement.GetString();
        var resource = _resources.FirstOrDefault(r => r.Uri == uri);
        
        if (resource == null)
        {
            throw new McpInvalidParamsException($"Resource not found: {uri}");
        }

        var content = await resource.Handler(ct);

        return new McpResourceReadResult
        {
            Contents = new List<McpResourceContent>
            {
                new McpTextResourceContent
                {
                    Uri = resource.Uri,
                    MimeType = resource.MimeType,
                    Text = content
                }
            }
        };
    }

    private McpPromptsListResult HandlePromptsList()
    {
        return new McpPromptsListResult
        {
            Prompts = _prompts.Select(p => new McpPromptSchema
            {
                Name = p.Name,
                Description = p.Description,
                Arguments = p.Arguments
            }).ToList()
        };
    }

    private async Task<McpPromptGetResult> HandlePromptGetAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null) throw new McpInvalidParamsException("Missing params");

        var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(@params.Value, _jsonOptions);
        if (paramsDict == null || !paramsDict.TryGetValue("name", out var nameElement))
        {
            throw new McpInvalidParamsException("Missing prompt name");
        }

        var promptName = nameElement.GetString();
        var prompt = _prompts.FirstOrDefault(p => p.Name == promptName);
        
        if (prompt == null)
        {
            throw new McpInvalidParamsException($"Prompt not found: {promptName}");
        }

        var arguments = paramsDict.TryGetValue("arguments", out var argsElement)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(argsElement, _jsonOptions)
            : new Dictionary<string, string>();

        var messages = await prompt.Handler(arguments ?? new Dictionary<string, string>(), ct);

        return new McpPromptGetResult
        {
            Description = prompt.Description,
            Messages = messages
        };
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

// Configuration and Options

public enum McpTransport { Stdio, HttpSse }

public sealed class McpServerOptions
{
    public string Name { get; init; } = "libr4-mcp-server";
    public string Version { get; init; } = "1.0.0";
    public McpTransport Transport { get; init; } = McpTransport.Stdio;
    public string? HttpUrl { get; init; }
}

// Tool definition

public sealed class McpTool
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public JsonElement InputSchema { get; init; }
    public Func<JsonElement, CancellationToken, Task<string>> Handler { get; init; } = null!;
}

// Resource definition

public sealed class McpResource
{
    public string Uri { get; init; } = "";
    public string Name { get; init; } = "";
    public string MimeType { get; init; } = "text/plain";
    public string? Description { get; init; }
    public Func<CancellationToken, Task<string>> Handler { get; init; } = null!;
}

// Prompt definition

public sealed class McpPrompt
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public List<McpPromptArgument>? Arguments { get; init; }
    public Func<Dictionary<string, string>, CancellationToken, Task<List<McpPromptMessage>>> Handler { get; init; } = null!;
}

public sealed class McpPromptArgument
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public bool Required { get; init; } = false;
}

// JSON-RPC types

public sealed class McpJsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
    
    [JsonPropertyName("id")]
    public int? Id { get; init; }
    
    [JsonPropertyName("method")]
    public string Method { get; init; } = "";
    
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

public sealed class McpJsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
    
    [JsonPropertyName("id")]
    public int? Id { get; init; }
    
    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }
    
    [JsonPropertyName("error")]
    public McpError? Error { get; init; }
}

public sealed class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }
    
    [JsonPropertyName("message")]
    public string Message { get; init; } = "";
    
    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}

// Result types

public sealed class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = "";
    
    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; init; } = null!;
    
    [JsonPropertyName("capabilities")]
    public McpServerCapabilities Capabilities { get; init; } = null!;
}

public sealed class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("version")]
    public string Version { get; init; } = "";
}

public sealed class McpServerCapabilities
{
    [JsonPropertyName("tools")]
    public object? Tools { get; init; }
    
    [JsonPropertyName("resources")]
    public object? Resources { get; init; }
    
    [JsonPropertyName("prompts")]
    public object? Prompts { get; init; }
}

public sealed class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public List<McpToolSchema> Tools { get; init; } = new();
}

public sealed class McpToolSchema
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
    
    [JsonPropertyName("inputSchema")]
    public JsonElement InputSchema { get; init; }
}

public sealed class McpToolCallResult
{
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; init; } = new();
    
    [JsonPropertyName("isError")]
    public bool IsError { get; init; }
}

public abstract class McpContent
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public sealed class McpTextContent : McpContent
{
    public override string Type => "text";
    
    [JsonPropertyName("text")]
    public string Text { get; init; } = "";
}

public sealed class McpResourcesListResult
{
    [JsonPropertyName("resources")]
    public List<McpResourceSchema> Resources { get; init; } = new();
}

public sealed class McpResourceSchema
{
    [JsonPropertyName("uri")]
    public string Uri { get; init; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; init; } = "";
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed class McpResourceReadResult
{
    [JsonPropertyName("contents")]
    public List<McpResourceContent> Contents { get; init; } = new();
}

public abstract class McpResourceContent
{
    [JsonPropertyName("uri")]
    public string Uri { get; init; } = "";
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; init; } = "";
}

public sealed class McpTextResourceContent : McpResourceContent
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = "";
}

public sealed class McpPromptsListResult
{
    [JsonPropertyName("prompts")]
    public List<McpPromptSchema> Prompts { get; init; } = new();
}

public sealed class McpPromptSchema
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("arguments")]
    public List<McpPromptArgument>? Arguments { get; init; }
}

public sealed class McpPromptGetResult
{
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("messages")]
    public List<McpPromptMessage> Messages { get; init; } = new();
}

public sealed class McpPromptMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = "";
    
    [JsonPropertyName("content")]
    public McpContent Content { get; init; } = null!;
}

// Exceptions

public class McpException : Exception
{
    public int Code { get; }
    
    public McpException(string message, int code) : base(message)
    {
        Code = code;
    }
}

public class McpMethodNotFoundException : McpException
{
    public McpMethodNotFoundException(string message) : base(message, -32601) { }
}

public class McpInvalidParamsException : McpException
{
    public McpInvalidParamsException(string message) : base(message, -32602) { }
}
*/
