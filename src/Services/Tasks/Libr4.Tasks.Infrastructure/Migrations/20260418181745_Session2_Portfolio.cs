using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_Portfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portfolio_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    ItemType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    SkillsUsed = table.Column<string>(type: "text", nullable: false),
                    Client = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProjectUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GithubUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LiveUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    Featured = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_items_CreatedAt",
                table: "portfolio_items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_items_Featured",
                table: "portfolio_items",
                column: "Featured");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_items_ItemType",
                table: "portfolio_items",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_items_Status",
                table: "portfolio_items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_items_UserId",
                table: "portfolio_items",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portfolio_items");
        }
    }
}
