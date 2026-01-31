using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget_Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class BudgetModuleRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_AnalyticalAccounts_AnalyticalAccountId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_AnalyticalAccountId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "AnalyticalAccountId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "PlannedAmount",
                table: "Budgets");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Budgets",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "ModifiedDate",
                table: "Budgets",
                newName: "RevisedDate");

            migrationBuilder.AddColumn<string>(
                name: "ArchivedBy",
                table: "Budgets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedDate",
                table: "Budgets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedBy",
                table: "Budgets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedDate",
                table: "Budgets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Budgets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisedBy",
                table: "Budgets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisedFromId",
                table: "Budgets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisedWithId",
                table: "Budgets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    AnalyticalAccountId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    BudgetedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetLines_AnalyticalAccounts_AnalyticalAccountId",
                        column: x => x.AnalyticalAccountId,
                        principalTable: "AnalyticalAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_RevisedFromId",
                table: "Budgets",
                column: "RevisedFromId",
                unique: true,
                filter: "[RevisedFromId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_AnalyticalAccountId",
                table: "BudgetLines",
                column: "AnalyticalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId_AnalyticalAccountId_Type",
                table: "BudgetLines",
                columns: new[] { "BudgetId", "AnalyticalAccountId", "Type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Budgets_RevisedFromId",
                table: "Budgets",
                column: "RevisedFromId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_Budgets_RevisedFromId",
                table: "Budgets");

            migrationBuilder.DropTable(
                name: "BudgetLines");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_RevisedFromId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "ArchivedDate",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "ConfirmedDate",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "RevisedBy",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "RevisedFromId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "RevisedWithId",
                table: "Budgets");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Budgets",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "RevisedDate",
                table: "Budgets",
                newName: "ModifiedDate");

            migrationBuilder.AddColumn<int>(
                name: "AnalyticalAccountId",
                table: "Budgets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Budgets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedAmount",
                table: "Budgets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_AnalyticalAccountId",
                table: "Budgets",
                column: "AnalyticalAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_AnalyticalAccounts_AnalyticalAccountId",
                table: "Budgets",
                column: "AnalyticalAccountId",
                principalTable: "AnalyticalAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
