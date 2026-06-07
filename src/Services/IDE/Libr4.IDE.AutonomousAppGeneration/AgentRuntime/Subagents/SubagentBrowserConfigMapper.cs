using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public static class SubagentBrowserConfigMapper
{
    public static SubagentBrowserConfig Map(string subagentId, AgentSpecBrowserSection section)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subagentId);
        ArgumentNullException.ThrowIfNull(section);

        return new SubagentBrowserConfig
        {
            SubagentId = subagentId,
            Enabled = section.Enabled,
            StealthMode = section.StealthMode,
            DefaultUserAgent = section.DefaultUserAgent,
            DefaultViewport = section.DefaultViewport is { Count: 2 }
                ? (section.DefaultViewport[0], section.DefaultViewport[1])
                : null,
            DefaultHeaders = section.DefaultHeaders,
            DataSelectors = section.DataSelectors.Select(MapSelector).ToList(),
            Tasks = section.Tasks.Select(MapTask).ToList()
        };
    }

    private static DataSelector MapSelector(AgentSpecDataSelector selector) =>
        new()
        {
            Name = selector.Name,
            Description = selector.Description,
            Selector = selector.Selector,
            Type = ParseSelectorType(selector.Type),
            Attribute = selector.Attribute
        };

    private static SubagentBrowserTaskDefinition MapTask(AgentSpecBrowserTask task)
    {
        var taskName = !string.IsNullOrWhiteSpace(task.TaskName)
            ? task.TaskName
            : task.Name;
        if (string.IsNullOrWhiteSpace(taskName))
            throw new InvalidOperationException("browser task requires taskName or name");

        return new SubagentBrowserTaskDefinition
        {
            TaskName = taskName,
            Description = task.Description,
            TimeoutSeconds = task.TimeoutSeconds,
            TakeScreenshots = task.TakeScreenshots,
            Actions = task.Actions.Select(MapAction).ToList(),
            ExtractionRules = task.ExtractionRules.Select(MapRule).ToList()
        };
    }

    private static BrowserActionTemplate MapAction(AgentSpecBrowserAction action) =>
        new()
        {
            Type = ParseActionType(action.Type),
            Selector = action.Selector,
            Value = action.Value,
            WaitMs = action.WaitMs
        };

    private static DataExtractionRule MapRule(AgentSpecExtractionRule rule) =>
        new()
        {
            FieldName = rule.FieldName,
            Type = ParseExtractionType(rule.Type),
            Selector = rule.Selector,
            Attribute = rule.Attribute,
            DefaultValue = rule.DefaultValue,
            Required = rule.Required
        };

    private static SelectorType ParseSelectorType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "xpath" => SelectorType.XPath,
            "javascript" or "js" => SelectorType.JavaScript,
            _ => SelectorType.Css
        };

    private static DataExtractionType ParseExtractionType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "attribute" => DataExtractionType.Attribute,
            "html" => DataExtractionType.Html,
            "count" => DataExtractionType.Count,
            "exists" => DataExtractionType.Exists,
            "javascript" or "js" => DataExtractionType.JavaScript,
            _ => DataExtractionType.Text
        };

    private static BrowserActionType ParseActionType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "navigate" => BrowserActionType.Navigate,
            "click" => BrowserActionType.Click,
            "type" => BrowserActionType.Type,
            "wait" => BrowserActionType.Wait,
            "wait_for_element" or "waitforelement" => BrowserActionType.WaitForElement,
            "screenshot" => BrowserActionType.Screenshot,
            "execute_script" or "executescript" => BrowserActionType.ExecuteScript,
            "get_content" or "getcontent" => BrowserActionType.GetContent,
            "scroll" => BrowserActionType.Scroll,
            _ => BrowserActionType.Wait
        };
}
