using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShivFurnitureERP.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetNameAndTypeToAnalyticalBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetName",
                table: "AnalyticalBudgets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BudgetType",
                table: "AnalyticalBudgets",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetName",
                table: "AnalyticalBudgets");

            migrationBuilder.DropColumn(
                name: "BudgetType",
                table: "AnalyticalBudgets");
        }
    }
}
