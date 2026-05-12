using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Matching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "matching");

            migrationBuilder.CreateTable(
                name: "Matches",
                schema: "matching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    FreelancerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalScore = table.Column<float>(type: "real", nullable: false),
                    KeywordScore = table.Column<float>(type: "real", nullable: false),
                    SemanticScore = table.Column<float>(type: "real", nullable: false),
                    MatchingSkills = table.Column<string>(type: "text", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    Feedback = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FeedbackAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScoringWeights",
                schema: "matching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    KeywordSkillWeight = table.Column<double>(type: "double precision", nullable: false),
                    SemanticWeight = table.Column<double>(type: "double precision", nullable: false),
                    ExperienceWeight = table.Column<double>(type: "double precision", nullable: false),
                    ReputationWeight = table.Column<double>(type: "double precision", nullable: false),
                    RecencyWeight = table.Column<double>(type: "double precision", nullable: false),
                    BudgetFitWeight = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringWeights", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringWeights_IsActive",
                schema: "matching",
                table: "ScoringWeights",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches",
                schema: "matching");

            migrationBuilder.DropTable(
                name: "ScoringWeights",
                schema: "matching");
        }
    }
}
