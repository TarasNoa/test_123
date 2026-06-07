using FluentAssertions;
using Libr4.IDE.Application.Obscura;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraTelemetryTests
{
    [Fact]
    public void RecordAction_DoesNotThrow()
    {
        var act = () => ObscuraTelemetry.RecordAction("browser_navigate", true);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEvidence_SkipsNonPositiveBytes()
    {
        var act = () => ObscuraTelemetry.RecordEvidence(0, "Screenshot");
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordPolicyDenial_DoesNotThrow()
    {
        var act = () => ObscuraTelemetry.RecordPolicyDenial("browser_navigate", "forbid");
        act.Should().NotThrow();
    }
}
