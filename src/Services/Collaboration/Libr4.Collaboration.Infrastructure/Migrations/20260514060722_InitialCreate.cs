using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Collaboration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "collaboration");

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "collaboration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CollaboratingUsers = table.Column<string>(type: "jsonb", nullable: false),
                    CollaborationRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permissions = table.Column<string>(type: "jsonb", nullable: false),
                    Versions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                schema: "collaboration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ActiveCallId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileShares = table.Column<string>(type: "jsonb", nullable: true),
                    Messages = table.Column<string>(type: "jsonb", nullable: true),
                    Participants = table.Column<string>(type: "jsonb", nullable: true),
                    Sessions = table.Column<string>(type: "jsonb", nullable: true),
                    Settings = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "video_calls",
                schema: "collaboration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsRecording = table.Column<bool>(type: "boolean", nullable: false),
                    CollaborationRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Participants = table.Column<string>(type: "jsonb", nullable: true),
                    Recordings = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_calls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_video_calls_rooms_CollaborationRoomId",
                        column: x => x.CollaborationRoomId,
                        principalSchema: "collaboration",
                        principalTable: "rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "whiteboards",
                schema: "collaboration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CollaborationRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentToolState = table.Column<string>(type: "jsonb", nullable: false),
                    Elements = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whiteboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_whiteboards_rooms_CollaborationRoomId",
                        column: x => x.CollaborationRoomId,
                        principalSchema: "collaboration",
                        principalTable: "rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_CollaborationRoomId",
                schema: "collaboration",
                table: "documents",
                column: "CollaborationRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_RoomId",
                schema: "collaboration",
                table: "documents",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_ActiveCallId",
                schema: "collaboration",
                table: "rooms",
                column: "ActiveCallId");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_CreatorId",
                schema: "collaboration",
                table: "rooms",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_Status",
                schema: "collaboration",
                table: "rooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_TaskId",
                schema: "collaboration",
                table: "rooms",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_video_calls_CollaborationRoomId",
                schema: "collaboration",
                table: "video_calls",
                column: "CollaborationRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_video_calls_RoomId",
                schema: "collaboration",
                table: "video_calls",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_whiteboards_CollaborationRoomId",
                schema: "collaboration",
                table: "whiteboards",
                column: "CollaborationRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_whiteboards_RoomId",
                schema: "collaboration",
                table: "whiteboards",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_rooms_CollaborationRoomId",
                schema: "collaboration",
                table: "documents",
                column: "CollaborationRoomId",
                principalSchema: "collaboration",
                principalTable: "rooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_video_calls_ActiveCallId",
                schema: "collaboration",
                table: "rooms",
                column: "ActiveCallId",
                principalSchema: "collaboration",
                principalTable: "video_calls",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_video_calls_rooms_CollaborationRoomId",
                schema: "collaboration",
                table: "video_calls");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "collaboration");

            migrationBuilder.DropTable(
                name: "whiteboards",
                schema: "collaboration");

            migrationBuilder.DropTable(
                name: "rooms",
                schema: "collaboration");

            migrationBuilder.DropTable(
                name: "video_calls",
                schema: "collaboration");
        }
    }
}
