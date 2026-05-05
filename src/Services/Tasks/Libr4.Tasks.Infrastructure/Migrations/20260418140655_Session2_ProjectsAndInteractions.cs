using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_ProjectsAndInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "follows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_follows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_likes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    ViewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_views", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookmarks_CreatedAt",
                table: "bookmarks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_bookmarks_TargetId",
                table: "bookmarks",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_bookmarks_UserId_TargetId_TargetType",
                table: "bookmarks",
                columns: new[] { "UserId", "TargetId", "TargetType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_follows_CreatedAt",
                table: "follows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_follows_FollowerId_FollowingId",
                table: "follows",
                columns: new[] { "FollowerId", "FollowingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_follows_FollowingId",
                table: "follows",
                column: "FollowingId");

            migrationBuilder.CreateIndex(
                name: "IX_likes_CreatedAt",
                table: "likes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_likes_TargetId",
                table: "likes",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_likes_UserId_TargetId_TargetType",
                table: "likes",
                columns: new[] { "UserId", "TargetId", "TargetType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_views_TargetId",
                table: "views",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_views_UserId_TargetId_TargetType",
                table: "views",
                columns: new[] { "UserId", "TargetId", "TargetType" });

            migrationBuilder.CreateIndex(
                name: "IX_views_ViewedAt",
                table: "views",
                column: "ViewedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookmarks");

            migrationBuilder.DropTable(
                name: "follows");

            migrationBuilder.DropTable(
                name: "likes");

            migrationBuilder.DropTable(
                name: "views");
        }
    }
}
