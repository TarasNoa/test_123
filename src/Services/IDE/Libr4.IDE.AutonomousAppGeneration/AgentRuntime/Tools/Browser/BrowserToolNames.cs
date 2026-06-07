namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;

/// <summary>Canonical browser_* tool names for subagent toolset specs.</summary>
public static class BrowserToolNames
{
    public const string Launch = "browser_launch";
    public const string Navigate = "browser_navigate";
    public const string Snapshot = "browser_snapshot";
    public const string Click = "browser_click";
    public const string Type = "browser_type";
    public const string Scroll = "browser_scroll";
    public const string Wait = "browser_wait";
    public const string Screenshot = "browser_screenshot";
    public const string ExecuteJs = "browser_execute_js";
    public const string Console = "browser_console";
    public const string GetContent = "browser_get_content";
    public const string Extract = "browser_extract";
    public const string Close = "browser_close";
    public const string RecordStart = "browser_record_start";
    public const string RecordStop = "browser_record_stop";
    public const string Research = "browser_research";

    public static readonly string[] All =
    [
        Launch, Navigate, Snapshot, Click, Type, Scroll, Wait,
        Screenshot, ExecuteJs, Console, GetContent, Extract,
        RecordStart, RecordStop, Close, Research
    ];
}
