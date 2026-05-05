using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libr4.Tasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeApplicationsToSeparateEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_applications_tasks_TaskAggregateId",
                table: "applications");

            migrationBuilder.DropIndex(
                name: "IX_applications_TaskAggregateId",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "TaskAggregateId",
                table: "applications");

            migrationBuilder.AddForeignKey(
                name: "FK_applications_tasks_TaskId",
                table: "applications",
                column: "TaskId",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_applications_tasks_TaskId",
                table: "applications");

            migrationBuilder.AddColumn<Guid>(
                name: "TaskAggregateId",
                table: "applications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_applications_TaskAggregateId",
                table: "applications",
                column: "TaskAggregateId");

            migrationBuilder.AddForeignKey(
                name: "FK_applications_tasks_TaskAggregateId",
                table: "applications",
                column: "TaskAggregateId",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
