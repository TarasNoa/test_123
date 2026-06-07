using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ScheduledAgentRunTests
{
    [Fact]
    public void FlowCronParser_DailyAt2Am_IsDue()
    {
        var slot = new DateTime(2026, 6, 6, 2, 0, 0, DateTimeKind.Utc);
        FlowCronParser.IsDue("0 2 * * *", slot, null).Should().BeTrue();
        FlowCronParser.IsDue("0 2 * * *", slot, slot.AddMinutes(-1)).Should().BeTrue();
        FlowCronParser.IsDue("0 2 * * *", slot, slot).Should().BeFalse();
    }

    [Fact]
    public void FlowCronParser_WrongHour_IsNotDue()
    {
        var slot = new DateTime(2026, 6, 6, 3, 0, 0, DateTimeKind.Utc);
        FlowCronParser.IsDue("0 2 * * *", slot, null).Should().BeFalse();
    }

    [Fact]
    public async Task Store_UpsertAndList_PersistsSchedule()
    {
        var path = Path.Combine(Path.GetTempPath(), "libr4-schedules-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var store = new SqliteScheduledAgentRunStore(
                Options.Create(new AgentSchedulingOptions { DbPath = path }),
                NullLogger<SqliteScheduledAgentRunStore>.Instance);
            await store.EnsureSchemaAsync();
            await store.UpsertAsync(new ScheduledAgentRunDefinition(
                "flow:test",
                "calorie-django-solidjs",
                "0 2 * * *",
                "/flow:calorie-django-solidjs nightly",
                8,
                true));

            var list = await store.ListAsync();
            list.Should().Contain(s => s.ScheduleId == "flow:test");
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void BuildScheduleId_NormalizesFlowName()
    {
        ScheduledAgentRunService.BuildScheduleId("Calorie-Django-SolidJS")
            .Should().Be("flow:calorie-django-solidjs");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best effort
        }
    }
}
