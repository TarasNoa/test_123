using System.Text.Json;

namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// Manages fork context for subagent isolation.
/// </summary>
public class ForkContextManager
{
    /// <summary>
    /// Builds fork context for a subagent.
    /// </summary>
    /// <param name="parentMessages">Parent conversation messages.</param>
    /// <param name="toolUseId">Tool use ID that triggered the subagent.</param>
    /// <param name="allowedTools">Tools available to the subagent.</param>
    /// <param name="disallowedTools">Tools explicitly disallowed for the subagent.</param>
    /// <returns>Fork context messages and prompt messages.</returns>
    public ForkContextResult BuildForkContext(
        List<object> parentMessages,
        string? toolUseId,
        HashSet<string> allowedTools,
        HashSet<string>? disallowedTools)
    {
        if (string.IsNullOrEmpty(toolUseId))
        {
            return new ForkContextResult
            {
                ForkContextMessages = new List<object>(),
                PromptMessages = new List<object>()
            };
        }

        // Find the tool use block in parent messages
        var (toolUseMessageIndex, toolUseMessage, taskToolUseBlock) = FindToolUseBlock(parentMessages, toolUseId);
        
        if (toolUseMessageIndex == -1 || toolUseMessage == null || taskToolUseBlock == null)
        {
            return new ForkContextResult
            {
                ForkContextMessages = new List<object>(),
                PromptMessages = new List<object>()
            };
        }

        // Get messages before the tool use
        var forkContextMessages = parentMessages.Take(toolUseMessageIndex).ToList();

        // Filter tool use blocks to only available tools
        var filteredMessages = FilterToolUseBlocks(forkContextMessages, allowedTools, disallowedTools);

        return new ForkContextResult
        {
            ForkContextMessages = filteredMessages,
            ToolUseMessage = toolUseMessage,
            ToolUseBlock = taskToolUseBlock
        };
    }

    /// <summary>
    /// Finds the tool use block in messages.
    /// </summary>
    private (int index, object? message, object? toolUseBlock) FindToolUseBlock(
        List<object> messages,
        string toolUseId)
    {
        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            
            // Check if this is an assistant message with content blocks
            if (IsAssistantMessage(msg))
            {
                var blocks = GetContentBlocks(msg);
                if (blocks != null)
                {
                    var match = blocks.FirstOrDefault(b => IsToolUseBlock(b) && GetToolUseId(b) == toolUseId);
                    if (match != null)
                    {
                        return (i, msg, match);
                    }
                }
            }
        }

        return (-1, null, null);
    }

    /// <summary>
    /// Filters tool use blocks to only available tools.
    /// </summary>
    private List<object> FilterToolUseBlocks(
        List<object> messages,
        HashSet<string> allowedTools,
        HashSet<string>? disallowedTools)
    {
        var filtered = new List<object>();

        foreach (var msg in messages)
        {
            if (!IsAssistantMessage(msg))
            {
                filtered.Add(msg);
                continue;
            }

            var blocks = GetContentBlocks(msg);
            if (blocks == null)
            {
                filtered.Add(msg);
                continue;
            }

            var filteredBlocks = new List<object>();
            foreach (var block in blocks)
            {
                if (!IsToolUseBlock(block))
                {
                    filteredBlocks.Add(block);
                    continue;
                }

                var toolName = GetToolName(block);
                if (string.IsNullOrEmpty(toolName))
                {
                    filteredBlocks.Add(block);
                    continue;
                }

                // Check if tool is allowed
                bool isAllowed = allowedTools.Contains("*") || allowedTools.Contains(toolName);
                
                // Check if tool is disallowed
                bool isDisallowed = disallowedTools != null && disallowedTools.Contains(toolName);

                if (isAllowed && !isDisallowed)
                {
                    filteredBlocks.Add(block);
                }
            }

            if (filteredBlocks.Count > 0)
            {
                filtered.Add(CreateFilteredMessage(msg, filteredBlocks));
            }
        }

        return filtered;
    }

    private bool IsAssistantMessage(object message)
    {
        // Check if message has type "assistant"
        var dict = message as IDictionary<string, object>;
        if (dict != null && dict.TryGetValue("type", out var type))
        {
            return type?.ToString() == "assistant";
        }
        return false;
    }

    private List<object>? GetContentBlocks(object message)
    {
        var dict = message as IDictionary<string, object>;
        if (dict != null && dict.TryGetValue("content", out var content))
        {
            if (content is List<object> blocks)
            {
                return blocks;
            }
        }
        return null;
    }

    private bool IsToolUseBlock(object block)
    {
        var dict = block as IDictionary<string, object>;
        if (dict != null && dict.TryGetValue("type", out var type))
        {
            return type?.ToString() == "tool_use";
        }
        return false;
    }

    private string? GetToolUseId(object block)
    {
        var dict = block as IDictionary<string, object>;
        if (dict != null && dict.TryGetValue("id", out var id))
        {
            return id?.ToString();
        }
        return null;
    }

    private string? GetToolName(object block)
    {
        var dict = block as IDictionary<string, object>;
        if (dict != null && dict.TryGetValue("name", out var name))
        {
            return name?.ToString();
        }
        return null;
    }

    private object CreateFilteredMessage(object originalMessage, List<object> filteredBlocks)
    {
        var dict = originalMessage as IDictionary<string, object>;
        if (dict != null)
        {
            var newDict = new Dictionary<string, object>(dict);
            newDict["content"] = filteredBlocks;
            return newDict;
        }
        return originalMessage;
    }

    /// <summary>
    /// Creates a fork context header message.
    /// </summary>
    public string CreateForkContextHeader()
    {
        return @"### FORKING CONVERSATION CONTEXT ###
### ENTERING SUB-AGENT ROUTINE ###
Entered sub-agent context

PLEASE NOTE: 
- The messages above this point are from the main thread prior to sub-agent execution. They are provided as context only.
- Context messages may include tool_use blocks for tools that are not available in the sub-agent context. You should only use the tools specifically provided to you in the system prompt.
- Only complete the specific sub-agent task you have been assigned below.";
    }
}

/// <summary>
/// Result of building fork context.
/// </summary>
public class ForkContextResult
{
    /// <summary>
    /// Filtered fork context messages from parent.
    /// </summary>
    public List<object> ForkContextMessages { get; set; } = new();

    /// <summary>
    /// Prompt messages.
    /// </summary>
    public List<object> PromptMessages { get; set; } = new();

    /// <summary>
    /// The tool use message that triggered the subagent.
    /// </summary>
    public object? ToolUseMessage { get; set; }

    /// <summary>
    /// The specific tool use block.
    /// </summary>
    public object? ToolUseBlock { get; set; }
}
