using Libr4.Tasks.Domain.TimeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class TimeSessionConfig : IEntityTypeConfiguration<TimeSession>
{
    public void Configure(EntityTypeBuilder<TimeSession> b)
    {
        b.ToTable("time_sessions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.TaskId);
        b.HasIndex(x => x.ProjectId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.StartedAt);
        b.HasIndex(x => x.StoppedAt);

        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.StopReason).HasMaxLength(100);
        b.Property(x => x.AntiCheatFingerprint).HasMaxLength(64);
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.Timezone).HasMaxLength(50);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.ComputerInfo).HasColumnType("jsonb");

        b.HasMany(x => x.TimeEntries)
            .WithOne()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Screenshots)
            .WithOne()
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.ActivityLogs)
            .WithOne()
            .HasForeignKey(l => l.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.AntiCheatAlerts)
            .WithOne()
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TimeEntryConfig : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> b)
    {
        b.ToTable("time_entries");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.SessionId);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.WorkType).HasMaxLength(50);
        b.Property(x => x.ProjectPhase).HasMaxLength(50);
        b.Property(x => x.MouseActivity).HasColumnType("jsonb");
        b.Property(x => x.KeyboardActivity).HasColumnType("jsonb");
        b.Property(x => x.ApplicationActivity).HasColumnType("jsonb");
        b.Property(x => x.ValidationDetails).HasColumnType("jsonb");
    }
}

public sealed class ScreenshotConfig : IEntityTypeConfiguration<Screenshot>
{
    public void Configure(EntityTypeBuilder<Screenshot> b)
    {
        b.ToTable("screenshots");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.SessionId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CapturedAt);

        b.Property(x => x.ImageData).HasMaxLength(int.MaxValue);
        b.Property(x => x.ImageHash).HasMaxLength(64);
        b.Property(x => x.Format).HasMaxLength(10).IsRequired();
        b.Property(x => x.WindowTitle).HasMaxLength(500);
        b.Property(x => x.FlaggedReason).HasMaxLength(5000);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.AnalysisResult).HasColumnType("jsonb");
        b.Property(x => x.ActiveApps).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.SuspiciousElements).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
    }
}

public sealed class ActivityLogConfig : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> b)
    {
        b.ToTable("activity_logs");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.SessionId);
        b.HasIndex(x => x.Timestamp);

        b.Property(x => x.ActivityType).HasMaxLength(50).IsRequired();
        b.Property(x => x.WindowFocus).HasMaxLength(200);
        b.Property(x => x.Details).HasColumnType("jsonb");
        b.Property(x => x.Metadata).HasColumnType("jsonb");
        b.Property(x => x.NetworkActivity).HasColumnType("jsonb");
        b.Property(x => x.MousePosition).HasColumnType("jsonb");
        b.Property(x => x.KeyboardState).HasColumnType("jsonb");
    }
}

public sealed class AntiCheatAlertConfig : IEntityTypeConfiguration<AntiCheatAlert>
{
    public void Configure(EntityTypeBuilder<AntiCheatAlert> b)
    {
        b.ToTable("anti_cheat_alerts");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.SessionId);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.AlertType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Severity).HasConversion<string>();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.Resolution).HasMaxLength(5000);
        b.Property(x => x.PenaltyApplied).HasMaxLength(100);
        b.Property(x => x.Details).HasColumnType("jsonb");
        b.Property(x => x.Evidence).HasColumnType("jsonb");
        b.Property(x => x.ActionsTaken).HasColumnType("jsonb");
    }
}

public sealed class TimeReportConfig : IEntityTypeConfiguration<TimeReport>
{
    public void Configure(EntityTypeBuilder<TimeReport> b)
    {
        b.ToTable("time_reports");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.StartDate);
        b.HasIndex(x => x.EndDate);

        b.Property(x => x.ReportType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.ProjectBreakdown).HasColumnType("jsonb");
        b.Property(x => x.TaskBreakdown).HasColumnType("jsonb");
        b.Property(x => x.DailyBreakdown).HasColumnType("jsonb");
        b.Property(x => x.HourlyBreakdown).HasColumnType("jsonb");
    }
}

public sealed class TimeTrackingSettingsConfig : IEntityTypeConfiguration<TimeTrackingSettings>
{
    public void Configure(EntityTypeBuilder<TimeTrackingSettings> b)
    {
        b.ToTable("time_tracking_settings");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();

        b.Property(x => x.ReportFrequency).HasMaxLength(20).IsRequired();
        b.Property(x => x.ExcludeApps).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
    }
}
