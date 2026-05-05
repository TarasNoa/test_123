using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_TeamsPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Documents = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BusinessAddress = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    BusinessPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BusinessEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BusinessType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    VerificationNotes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BadgeLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BadgeUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_verifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rate_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateType = table.Column<string>(type: "text", nullable: false),
                    RateAmount = table.Column<float>(type: "real", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    ProjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    ReasonForChange = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rate_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_tests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    PassingScore = table.Column<float>(type: "real", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    Questions = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Instructions = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Resources = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresProctoring = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptsCount = table.Column<int>(type: "integer", nullable: false),
                    PassRate = table.Column<float>(type: "real", nullable: true),
                    AverageScore = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_tests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "team_portfolio_analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    UniqueViews = table.Column<int>(type: "integer", nullable: false),
                    Clicks = table.Column<int>(type: "integer", nullable: false),
                    Conversions = table.Column<int>(type: "integer", nullable: false),
                    AverageViewDuration = table.Column<float>(type: "real", nullable: true),
                    BounceRate = table.Column<float>(type: "real", nullable: true),
                    ViewsByCountry = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    ViewsBySource = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    DailyViews = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_portfolio_analytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "team_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverallScore = table.Column<float>(type: "real", nullable: false),
                    CriteriaScores = table.Column<Dictionary<string, float>>(type: "jsonb", nullable: false),
                    ReviewText = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Strengths = table.Column<string>(type: "text", nullable: false),
                    Improvements = table.Column<string>(type: "text", nullable: false),
                    WouldHireAgain = table.Column<bool>(type: "boolean", nullable: true),
                    WouldRecommend = table.Column<bool>(type: "boolean", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseText = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RespondedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    HelpfulVotes = table.Column<int>(type: "integer", nullable: false),
                    ReportCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Tagline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Languages = table.Column<string>(type: "text", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    Industries = table.Column<string>(type: "text", nullable: false),
                    Categories = table.Column<string>(type: "text", nullable: false),
                    MinProjectSize = table.Column<int>(type: "integer", nullable: true),
                    HourlyRateMin = table.Column<float>(type: "real", nullable: true),
                    HourlyRateMax = table.Column<float>(type: "real", nullable: true),
                    PreferredRateType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CompletedProjects = table.Column<int>(type: "integer", nullable: false),
                    TotalEarnings = table.Column<float>(type: "real", nullable: false),
                    AverageRating = table.Column<float>(type: "real", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BrandColors = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_test_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    Answers = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    TimeTakenSeconds = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_test_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skill_test_results_skill_tests_TestId",
                        column: x => x.TestId,
                        principalTable: "skill_tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bio = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Permissions = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProjectsContributed = table.Column<int>(type: "integer", nullable: false),
                    EarningsContributed = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_members_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_portfolio_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    Images = table.Column<string>(type: "text", nullable: false),
                    Videos = table.Column<string>(type: "text", nullable: false),
                    Files = table.Column<string>(type: "text", nullable: false),
                    ProjectUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ClientTestimonial = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ProjectDuration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Technologies = table.Column<string>(type: "text", nullable: false),
                    ToolsUsed = table.Column<string>(type: "text", nullable: false),
                    Methodologies = table.Column<string>(type: "text", nullable: false),
                    BudgetRange = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    TeamSize = table.Column<int>(type: "integer", nullable: true),
                    RoleInProject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    ShareCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_portfolio_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_portfolio_items_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_client_verifications_Status",
                table: "client_verifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_client_verifications_SubmittedAt",
                table: "client_verifications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_client_verifications_UserId",
                table: "client_verifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_rate_history_EffectiveDate",
                table: "rate_history",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_rate_history_IsCurrent",
                table: "rate_history",
                column: "IsCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_rate_history_UserId",
                table: "rate_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_test_results_CompletedAt",
                table: "skill_test_results",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_skill_test_results_Passed",
                table: "skill_test_results",
                column: "Passed");

            migrationBuilder.CreateIndex(
                name: "IX_skill_test_results_TestId",
                table: "skill_test_results",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_test_results_UserId",
                table: "skill_test_results",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_tests_Category",
                table: "skill_tests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_skill_tests_CreatedAt",
                table: "skill_tests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_skill_tests_IsActive",
                table: "skill_tests",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_Status",
                table: "team_members",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_TeamId",
                table: "team_members",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_UserId",
                table: "team_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_portfolio_analytics_PortfolioItemId",
                table: "team_portfolio_analytics",
                column: "PortfolioItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_portfolio_analytics_UpdatedAt",
                table: "team_portfolio_analytics",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_team_portfolio_items_CreatedAt",
                table: "team_portfolio_items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_team_portfolio_items_IsPublic",
                table: "team_portfolio_items",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_team_portfolio_items_TeamId",
                table: "team_portfolio_items",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_team_portfolio_items_UserId",
                table: "team_portfolio_items",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_reviews_CreatedAt",
                table: "team_reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_team_reviews_IsPublic",
                table: "team_reviews",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_team_reviews_ReviewerId",
                table: "team_reviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_team_reviews_TargetId",
                table: "team_reviews",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_team_reviews_TaskId",
                table: "team_reviews",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_CreatedAt",
                table: "teams",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_teams_CreatedBy",
                table: "teams",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_teams_IsActive",
                table: "teams",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_teams_IsVerified",
                table: "teams",
                column: "IsVerified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_verifications");

            migrationBuilder.DropTable(
                name: "rate_history");

            migrationBuilder.DropTable(
                name: "skill_test_results");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "team_portfolio_analytics");

            migrationBuilder.DropTable(
                name: "team_portfolio_items");

            migrationBuilder.DropTable(
                name: "team_reviews");

            migrationBuilder.DropTable(
                name: "skill_tests");

            migrationBuilder.DropTable(
                name: "teams");
        }
    }
}
