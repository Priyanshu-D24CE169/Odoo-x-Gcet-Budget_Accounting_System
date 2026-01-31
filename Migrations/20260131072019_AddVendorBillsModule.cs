using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget_Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorBillsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "VendorBillLines");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "VendorBillLines",
                newName: "LineTotal");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "VendorBills",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "VendorBills");

            migrationBuilder.RenameColumn(
                name: "LineTotal",
                table: "VendorBillLines",
                newName: "SubTotal");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "VendorBillLines",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
