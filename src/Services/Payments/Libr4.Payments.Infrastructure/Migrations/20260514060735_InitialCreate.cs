using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.CreateTable(
                name: "escrows",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    FreelancerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_escrows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvoiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    StripePaymentMethodId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExpMonth = table.Column<int>(type: "integer", nullable: true),
                    ExpYear = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StripeChargeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HeldBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_entries",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_entries_wallets_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "payments",
                        principalTable: "wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_escrows_ClientId",
                schema: "payments",
                table: "escrows",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_escrows_FreelancerId",
                schema: "payments",
                table: "escrows",
                column: "FreelancerId");

            migrationBuilder.CreateIndex(
                name: "IX_escrows_Status",
                schema: "payments",
                table: "escrows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_escrows_TaskId",
                schema: "payments",
                table: "escrows",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_fraud_history_user_id",
                table: "fraud_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_fraud_history_user_time",
                table: "fraud_history",
                columns: new[] { "UserId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_StripePaymentMethodId",
                schema: "payments",
                table: "payment_methods",
                column: "StripePaymentMethodId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_UserId",
                schema: "payments",
                table: "payment_methods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_UserId_IsDefault",
                schema: "payments",
                table: "payment_methods",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_CreatedAt",
                schema: "payments",
                table: "transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Status",
                schema: "payments",
                table: "transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_StripePaymentIntentId",
                schema: "payments",
                table: "transactions",
                column: "StripePaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Type",
                schema: "payments",
                table: "transactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_UserId",
                schema: "payments",
                table: "transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_entries_CreatedAt",
                schema: "payments",
                table: "wallet_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_entries_TransactionId",
                schema: "payments",
                table: "wallet_entries",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_entries_WalletId",
                schema: "payments",
                table: "wallet_entries",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_UserId_Currency",
                schema: "payments",
                table: "wallets",
                columns: new[] { "UserId", "Currency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "escrows",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "fraud_history");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "transactions",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "wallet_entries",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "wallets",
                schema: "payments");
        }
    }
}
