using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Session2_CRM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crm_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompanySize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    FoundedYear = table.Column<int>(type: "integer", nullable: true),
                    Revenue = table.Column<decimal>(type: "numeric", nullable: true),
                    Employees = table.Column<int>(type: "integer", nullable: true),
                    SubscriptionPlan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AiConfiguration = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    AutomationSettings = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ContactsCount = table.Column<int>(type: "integer", nullable: false),
                    DealsCount = table.Column<int>(type: "integer", nullable: false),
                    TasksCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crm_activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ActivityDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_activities_crm_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "crm_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crm_contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LinkedIn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    CustomFields = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    LeadScore = table.Column<int>(type: "integer", nullable: false),
                    LeadSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LeadStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PreferredContactMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DoNotCall = table.Column<bool>(type: "boolean", nullable: false),
                    DoNotEmail = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastContactedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_contacts_crm_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "crm_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crm_deals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Probability = table.Column<int>(type: "integer", nullable: true),
                    WeightedAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: true),
                    StageOrder = table.Column<int>(type: "integer", nullable: true),
                    ExpectedCloseDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualCloseDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Competitors = table.Column<string>(type: "text", nullable: false),
                    NextSteps = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    LossReason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CustomFields = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsWon = table.Column<bool>(type: "boolean", nullable: false),
                    IsLost = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_deals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_deals_crm_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "crm_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crm_pipelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Stages = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AutoAdvance = table.Column<bool>(type: "boolean", nullable: false),
                    TotalDeals = table.Column<int>(type: "integer", nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ConversionRate = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_pipelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_pipelines_crm_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "crm_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crm_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_tasks_crm_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "crm_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_accounts_CompanyName",
                table: "crm_accounts",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_crm_accounts_CreatedAt",
                table: "crm_accounts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_accounts_OwnerId",
                table: "crm_accounts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_accounts_Status",
                table: "crm_accounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_crm_activities_AccountId",
                table: "crm_activities",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_activities_ActivityDate",
                table: "crm_activities",
                column: "ActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_crm_activities_ActivityType",
                table: "crm_activities",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_crm_activities_ContactId",
                table: "crm_activities",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_activities_CreatedAt",
                table: "crm_activities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_activities_DealId",
                table: "crm_activities",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_contacts_AccountId",
                table: "crm_contacts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_contacts_CreatedAt",
                table: "crm_contacts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_contacts_Email",
                table: "crm_contacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_crm_deals_AccountId",
                table: "crm_deals",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_deals_ContactId",
                table: "crm_deals",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_deals_CreatedAt",
                table: "crm_deals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_deals_Stage",
                table: "crm_deals",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_crm_pipelines_AccountId",
                table: "crm_pipelines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_pipelines_CreatedAt",
                table: "crm_pipelines",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_pipelines_IsActive",
                table: "crm_pipelines",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_crm_pipelines_IsDefault",
                table: "crm_pipelines",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_crm_tasks_AccountId",
                table: "crm_tasks",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_tasks_CreatedAt",
                table: "crm_tasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_tasks_DueDate",
                table: "crm_tasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_crm_tasks_Priority",
                table: "crm_tasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_crm_tasks_Status",
                table: "crm_tasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_activities");

            migrationBuilder.DropTable(
                name: "crm_contacts");

            migrationBuilder.DropTable(
                name: "crm_deals");

            migrationBuilder.DropTable(
                name: "crm_pipelines");

            migrationBuilder.DropTable(
                name: "crm_tasks");

            migrationBuilder.DropTable(
                name: "crm_accounts");
        }
    }
}
