using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget_Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrdersAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "CustomerInvoiceLines");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "CustomerInvoiceLines",
                newName: "LineTotal");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "CustomerInvoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "CustomerInvoices");

            migrationBuilder.RenameColumn(
                name: "LineTotal",
                table: "CustomerInvoiceLines",
                newName: "SubTotal");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CustomerInvoiceLines",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
