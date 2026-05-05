using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_DisputeResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespondentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    DisputeAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ResolutionRequested = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    AssignedModeratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedArbitratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModeratorAssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EscalatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalOutcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AiAnalysis = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    AiConfidence = table.Column<float>(type: "real", nullable: true),
                    EvidenceFiles = table.Column<string>(type: "text", nullable: false),
                    EscalationReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    DismissalReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disputes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dispute_arbitrators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArbitratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignmentReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Specialization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FeeRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Decision = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    DecisionReasoning = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_arbitrators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispute_arbitrators_disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispute_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EvidenceData = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    FileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerificationNotes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    IsAdmissible = table.Column<bool>(type: "boolean", nullable: false),
                    InadmissibilityReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispute_evidence_disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispute_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EvidenceFiles = table.Column<string>(type: "text", nullable: false),
                    Attachments = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    ParentMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispute_messages_disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispute_resolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolutionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolutionTerms = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    AmountRefund = table.Column<decimal>(type: "numeric", nullable: true),
                    AmountPenalty = table.Column<decimal>(type: "numeric", nullable: true),
                    AmountCompensation = table.Column<decimal>(type: "numeric", nullable: true),
                    AdditionalActions = table.Column<string>(type: "text", nullable: false),
                    Deadlines = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Response = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ResponderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CounterTerms = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ResponseReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AiAnalysis = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    FairnessScore = table.Column<int>(type: "integer", nullable: true),
                    AcceptanceLikelihood = table.Column<int>(type: "integer", nullable: true),
                    ProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_resolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispute_resolutions_disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dispute_arbitrators_ArbitratorId",
                table: "dispute_arbitrators",
                column: "ArbitratorId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_arbitrators_AssignedAt",
                table: "dispute_arbitrators",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_arbitrators_DisputeId",
                table: "dispute_arbitrators",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_arbitrators_Status",
                table: "dispute_arbitrators",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_DisputeId",
                table: "dispute_evidence",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_Status",
                table: "dispute_evidence",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_SubmittedAt",
                table: "dispute_evidence",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_SubmittedBy",
                table: "dispute_evidence",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_messages_CreatedAt",
                table: "dispute_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_messages_DisputeId",
                table: "dispute_messages",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_messages_SenderId",
                table: "dispute_messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_DisputeId",
                table: "dispute_resolutions",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_ProposedAt",
                table: "dispute_resolutions",
                column: "ProposedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_ProposerId",
                table: "dispute_resolutions",
                column: "ProposerId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_Status",
                table: "dispute_resolutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_CreatedAt",
                table: "disputes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_InitiatorId",
                table: "disputes",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_Priority",
                table: "disputes",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_ResolvedAt",
                table: "disputes",
                column: "ResolvedAt");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_RespondentId",
                table: "disputes",
                column: "RespondentId");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_Severity",
                table: "disputes",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_Status",
                table: "disputes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_TaskId",
                table: "disputes",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dispute_arbitrators");

            migrationBuilder.DropTable(
                name: "dispute_evidence");

            migrationBuilder.DropTable(
                name: "dispute_messages");

            migrationBuilder.DropTable(
                name: "dispute_resolutions");

            migrationBuilder.DropTable(
                name: "disputes");
        }
    }
}
