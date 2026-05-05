using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session1_SkillCalibration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skill_calibrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CurrentDifficulty = table.Column<double>(type: "double precision", precision: 5, scale: 4, nullable: false),
                    TotalAttempts = table.Column<int>(type: "integer", nullable: false),
                    PassedAttempts = table.Column<int>(type: "integer", nullable: false),
                    PassRate = table.Column<double>(type: "double precision", precision: 5, scale: 4, nullable: false),
                    AverageScore = table.Column<double>(type: "double precision", precision: 10, scale: 2, nullable: false),
                    LastCalibrationAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_calibrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_skill_calibrations_SkillTestId",
                table: "skill_calibrations",
                column: "SkillTestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_calibrations");
        }
    }
}
