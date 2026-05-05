using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_WorkDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    FreelancerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PreviewType = table.Column<string>(type: "text", nullable: true),
                    PreviewUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreviewContainerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PreviewStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PreviewEndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousDeliveryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    PaymentCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AutoPayOnApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequireClientApproval = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPreviewDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ExtraData = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_deliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "preview_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreviewUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebsocketUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContainerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContainerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Port = table.Column<int>(type: "integer", nullable: true),
                    CpuLimit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MemoryLimit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MaxDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InteractionsCount = table.Column<int>(type: "integer", nullable: false),
                    InteractionsLog = table.Column<List<Dictionary<string, object>>>(type: "jsonb", nullable: false),
                    ClientNotes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ErrorDetails = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preview_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_preview_sessions_work_deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "work_deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_delivery_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFilename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsEntryPoint = table.Column<bool>(type: "boolean", nullable: false),
                    IsScanned = table.Column<bool>(type: "boolean", nullable: false),
                    ScanResult = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ScanDetails = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    ContentPreview = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_delivery_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_delivery_files_work_deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "work_deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_preview_sessions_ClientId",
                table: "preview_sessions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_preview_sessions_CreatedAt",
                table: "preview_sessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_preview_sessions_DeliveryId",
                table: "preview_sessions",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_preview_sessions_SessionToken",
                table: "preview_sessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_deliveries_ClientId",
                table: "work_deliveries",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_work_deliveries_CreatedAt",
                table: "work_deliveries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_work_deliveries_FreelancerId",
                table: "work_deliveries",
                column: "FreelancerId");

            migrationBuilder.CreateIndex(
                name: "IX_work_deliveries_Status",
                table: "work_deliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_work_deliveries_SubmittedAt",
                table: "work_deliveries",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_work_deliveries_TaskId",
                table: "work_deliveries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_work_delivery_files_DeliveryId",
                table: "work_delivery_files",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_work_delivery_files_UploadedAt",
                table: "work_delivery_files",
                column: "UploadedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "preview_sessions");

            migrationBuilder.DropTable(
                name: "work_delivery_files");

            migrationBuilder.DropTable(
                name: "work_deliveries");
        }
    }
}
