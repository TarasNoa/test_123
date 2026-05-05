namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;

public enum AgentEventType
{
    BuildStart,
    BuildComplete,
    TestStart,
    TestComplete,
    SecurityScanStart,
    SecurityScanComplete,
    TerminalOutput,
    // Obscura browser events
    BrowserLaunch,
    BrowserNavigate,
    BrowserScreenshot,
    BrowserExecuteJavaScript,
    BrowserClose,
}
