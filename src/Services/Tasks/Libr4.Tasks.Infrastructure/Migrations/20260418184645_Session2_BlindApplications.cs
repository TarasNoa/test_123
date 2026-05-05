using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_BlindApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blind_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnonymousId = table.Column<string>(type: "text", nullable: false),
                    ProposalText = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CoverLetter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PortfolioLinks = table.Column<string>(type: "text", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    FixedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedHours = table.Column<int>(type: "integer", nullable: true),
                    EstimatedDays = table.Column<int>(type: "integer", nullable: true),
                    Availability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AnonymizedProfile = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    SkillTags = table.Column<string>(type: "text", nullable: false),
                    ExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QualityScore = table.Column<double>(type: "double precision", nullable: false),
                    BiasScore = table.Column<double>(type: "double precision", nullable: false),
                    AiMatchScore = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ClientNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevealedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blind_applications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blind_applications_AnonymousId",
                table: "blind_applications",
                column: "AnonymousId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blind_applications_ApplicantId",
                table: "blind_applications",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_blind_applications_RevealedAt",
                table: "blind_applications",
                column: "RevealedAt");

            migrationBuilder.CreateIndex(
                name: "IX_blind_applications_Status",
                table: "blind_applications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_blind_applications_SubmittedAt",
                table: "blind_applications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_blind_applications_TaskId",
                table: "blind_applications",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blind_applications");
        }
    }
}
