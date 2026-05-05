using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_TimeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "time_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    TotalMinutes = table.Column<float>(type: "real", nullable: false),
                    TotalEarnings = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgHourlyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    ProjectBreakdown = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    TaskBreakdown = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    DailyBreakdown = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    HourlyBreakdown = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    AvgActivityLevel = table.Column<float>(type: "real", nullable: true),
                    AvgValidationScore = table.Column<float>(type: "real", nullable: true),
                    TotalScreenshots = table.Column<int>(type: "integer", nullable: false),
                    FlaggedActivities = table.Column<int>(type: "integer", nullable: false),
                    EfficiencyRate = table.Column<float>(type: "real", nullable: true),
                    IdlePercentage = table.Column<float>(type: "real", nullable: true),
                    ProductivityScore = table.Column<float>(type: "real", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "time_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StoppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<float>(type: "real", nullable: true),
                    TotalMinutes = table.Column<float>(type: "real", nullable: false),
                    IdleMinutes = table.Column<float>(type: "real", nullable: false),
                    TotalEarnings = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StopReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ComputerInfo = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    AntiCheatFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ScreenshotEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ActivityTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPauseEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "time_tracking_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScreenshotEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ScreenshotInterval = table.Column<int>(type: "integer", nullable: false),
                    ScreenshotQuality = table.Column<int>(type: "integer", nullable: false),
                    BlurScreenshots = table.Column<bool>(type: "boolean", nullable: false),
                    ActivityTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MouseTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    KeyboardTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AppTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPauseEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    InactivityTimeout = table.Column<int>(type: "integer", nullable: false),
                    AutoPauseMinDuration = table.Column<int>(type: "integer", nullable: false),
                    AntiCheatEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    StrictValidation = table.Column<bool>(type: "boolean", nullable: false),
                    AlertThreshold = table.Column<float>(type: "real", nullable: false),
                    PrivateMode = table.Column<bool>(type: "boolean", nullable: false),
                    ExcludeApps = table.Column<string>(type: "text", nullable: false),
                    DataRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IdleAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ScreenshotAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoReportsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReportFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IncludeScreenshots = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_tracking_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "activity_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CpuUsage = table.Column<float>(type: "real", nullable: true),
                    MemoryUsage = table.Column<float>(type: "real", nullable: true),
                    NetworkActivity = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    MousePosition = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    KeyboardState = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    WindowFocus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activity_logs_time_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "time_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "anti_cheat_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Details = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Evidence = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionsTaken = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    PenaltyApplied = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anti_cheat_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_anti_cheat_alerts_time_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "time_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "screenshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageData = table.Column<string>(type: "text", maxLength: 2147483647, nullable: false),
                    FileSize = table.Column<int>(type: "integer", nullable: false),
                    ImageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Quality = table.Column<int>(type: "integer", nullable: true),
                    ActivityLevel = table.Column<float>(type: "real", nullable: true),
                    ActiveApps = table.Column<string>(type: "text", nullable: false),
                    WindowTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AnalysisResult = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    BlurrinessScore = table.Column<float>(type: "real", nullable: true),
                    SuspiciousElements = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FlaggedReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_screenshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_screenshots_time_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "time_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationMinutes = table.Column<float>(type: "real", nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ActivityLevel = table.Column<float>(type: "real", nullable: true),
                    MouseActivity = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    KeyboardActivity = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    ApplicationActivity = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    ValidationScore = table.Column<int>(type: "integer", nullable: true),
                    ValidationDetails = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    WorkType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProjectPhase = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_time_entries_time_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "time_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_SessionId",
                table: "activity_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_Timestamp",
                table: "activity_logs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_anti_cheat_alerts_CreatedAt",
                table: "anti_cheat_alerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_anti_cheat_alerts_SessionId",
                table: "anti_cheat_alerts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_anti_cheat_alerts_Status",
                table: "anti_cheat_alerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_anti_cheat_alerts_UserId",
                table: "anti_cheat_alerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_screenshots_CapturedAt",
                table: "screenshots",
                column: "CapturedAt");

            migrationBuilder.CreateIndex(
                name: "IX_screenshots_SessionId",
                table: "screenshots",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_screenshots_Status",
                table: "screenshots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_time_entries_CreatedAt",
                table: "time_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_time_entries_SessionId",
                table: "time_entries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_time_reports_EndDate",
                table: "time_reports",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_time_reports_StartDate",
                table: "time_reports",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_time_reports_UserId",
                table: "time_reports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_time_sessions_ProjectId",
                table: "time_sessions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_time_sessions_StartedAt",
                table: "time_sessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_time_sessions_Status",
                table: "time_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_time_sessions_StoppedAt",
                table: "time_sessions",
                column: "StoppedAt");

            migrationBuilder.CreateIndex(
                name: "IX_time_sessions_TaskId",
                table: "time_sessions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_time_sessions_UserId",
                table: "time_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_time_tracking_settings_UserId",
                table: "time_tracking_settings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_logs");

            migrationBuilder.DropTable(
                name: "anti_cheat_alerts");

            migrationBuilder.DropTable(
                name: "screenshots");

            migrationBuilder.DropTable(
                name: "time_entries");

            migrationBuilder.DropTable(
                name: "time_reports");

            migrationBuilder.DropTable(
                name: "time_tracking_settings");

            migrationBuilder.DropTable(
                name: "time_sessions");
        }
    }
}
