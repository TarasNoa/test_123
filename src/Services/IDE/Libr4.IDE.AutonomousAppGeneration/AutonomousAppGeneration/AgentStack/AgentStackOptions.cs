namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public sealed class AgentStackOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentStack";

    /// <summary>Block host startup when required services are unhealthy.</summary>
    public bool RequireHealthyAtStartup { get; set; }

    /// <summary>Reject new generation runs when required services are unhealthy.</summary>
    public bool RequireHealthyBeforeRun { get; set; } = true;

    public bool EnableShadowSyncGate { get; set; } = true;

    public bool EnableSandboxControllerGate { get; set; } = true;

    public bool EnableSecurityScannerGate { get; set; } = true;

    public bool EnableQdrantGate { get; set; }

    public string ShadowSyncBaseUrl { get; set; } = "http://localhost:8080";

    public string SandboxControllerBaseUrl { get; set; } = "http://localhost:9090";

    public string SecurityScannerBaseUrl { get; set; } = "http://localhost:7070";

    public string QdrantBaseUrl { get; set; } = "http://localhost:6333";

    public int HealthCheckTimeoutSeconds { get; set; } = 8;
}
