using System.Diagnostics.Metrics;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AutoGenTelemetryTests
{
    [Fact]
    public void RunsStarted_Counter_IsObservable()
    {
        var captured = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == AutoGenTelemetry.MeterName && instrument.Name == "autogen.runs.started")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => captured.Add(value));
        listener.Start();

        AutoGenTelemetry.RunsStarted.Add(1);
        AutoGenTelemetry.RunsStarted.Add(1);

        captured.Should().HaveCountGreaterOrEqualTo(2);
        captured.Sum().Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void GateScore_Histogram_RecordsWithTags()
    {
        var captured = new List<(int value, string? stage)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == AutoGenTelemetry.MeterName && instrument.Name == "autogen.gate.score")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<int>((_, value, tags, _) =>
        {
            string? stage = null;
            foreach (var t in tags)
                if (t.Key == "stage") stage = t.Value?.ToString();
            captured.Add((value, stage));
        });
        listener.Start();

        AutoGenTelemetry.RecordGateScore(8, "plan");
        AutoGenTelemetry.RecordGateScore(10, "review2:post_generation");

        captured.Should().Contain(c => c.value == 8 && c.stage == "plan");
        captured.Should().Contain(c => c.value == 10 && c.stage == "review2:post_generation");
    }

    [Fact]
    public void StartActivity_DoesNotThrow_When_NoListener()
    {
        // Without a listener, ActivitySource.StartActivity returns null but must not throw.
        var act = AutoGenTelemetry.StartActivity("test_span");
        act?.Dispose();
    }

    [Fact]
    public void RecordFallback_TagsStackAndKind()
    {
        var captured = new List<(string? stack, string? kind)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == AutoGenTelemetry.MeterName && instrument.Name == "autogen.fallback.used")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string? stack = null, kind = null;
            foreach (var t in tags)
            {
                if (t.Key == "stack") stack = t.Value?.ToString();
                else if (t.Key == "kind") kind = t.Value?.ToString();
            }
            captured.Add((stack, kind));
        });
        listener.Start();

        AutoGenTelemetry.RecordFallback("python", "readme");

        captured.Should().Contain(c => c.stack == "python" && c.kind == "readme");
    }
}
