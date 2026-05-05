using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_Certificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CertificateType = table.Column<string>(type: "text", nullable: false),
                    IssuingOrganization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CertificateUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CredentialId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IssuedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    VerificationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "certificate_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_attachments_certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "certificate_endorsements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndorserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndorsementText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_endorsements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_endorsements_certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "certificate_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_verifications_certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_certificate_attachments_CertificateId",
                table: "certificate_attachments",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_attachments_UploadedAt",
                table: "certificate_attachments",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_endorsements_CertificateId",
                table: "certificate_endorsements",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_endorsements_CreatedAt",
                table: "certificate_endorsements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_endorsements_EndorserId",
                table: "certificate_endorsements",
                column: "EndorserId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_verifications_CertificateId",
                table: "certificate_verifications",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_verifications_CreatedAt",
                table: "certificate_verifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_verifications_VerifierId",
                table: "certificate_verifications",
                column: "VerifierId");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_CertificateType",
                table: "certificates",
                column: "CertificateType");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_CreatedAt",
                table: "certificates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_ExpiryDate",
                table: "certificates",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_IssuedDate",
                table: "certificates",
                column: "IssuedDate");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_Status",
                table: "certificates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_UserId",
                table: "certificates",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificate_attachments");

            migrationBuilder.DropTable(
                name: "certificate_endorsements");

            migrationBuilder.DropTable(
                name: "certificate_verifications");

            migrationBuilder.DropTable(
                name: "certificates");
        }
    }
}
