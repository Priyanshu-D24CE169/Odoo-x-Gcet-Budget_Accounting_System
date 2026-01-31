using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget_Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransactionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PurchaseOrderLines");

            migrationBuilder.RenameColumn(
                name: "OrderNumber",
                table: "SalesOrders",
                newName: "SONumber");

            migrationBuilder.RenameColumn(
                name: "OrderDate",
                table: "SalesOrders",
                newName: "SODate");

            migrationBuilder.RenameIndex(
                name: "IX_SalesOrders_OrderNumber",
                table: "SalesOrders",
                newName: "IX_SalesOrders_SONumber");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "SalesOrderLines",
                newName: "LineTotal");

            migrationBuilder.RenameColumn(
                name: "OrderNumber",
                table: "PurchaseOrders",
                newName: "PONumber");

            migrationBuilder.RenameColumn(
                name: "OrderDate",
                table: "PurchaseOrders",
                newName: "PODate");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                newName: "IX_PurchaseOrders_PONumber");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "PurchaseOrderLines",
                newName: "LineTotal");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "SalesOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "PurchaseOrders");

            migrationBuilder.RenameColumn(
                name: "SONumber",
                table: "SalesOrders",
                newName: "OrderNumber");

            migrationBuilder.RenameColumn(
                name: "SODate",
                table: "SalesOrders",
                newName: "OrderDate");

            migrationBuilder.RenameIndex(
                name: "IX_SalesOrders_SONumber",
                table: "SalesOrders",
                newName: "IX_SalesOrders_OrderNumber");

            migrationBuilder.RenameColumn(
                name: "LineTotal",
                table: "SalesOrderLines",
                newName: "SubTotal");

            migrationBuilder.RenameColumn(
                name: "PONumber",
                table: "PurchaseOrders",
                newName: "OrderNumber");

            migrationBuilder.RenameColumn(
                name: "PODate",
                table: "PurchaseOrders",
                newName: "OrderDate");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrders_PONumber",
                table: "PurchaseOrders",
                newName: "IX_PurchaseOrders_OrderNumber");

            migrationBuilder.RenameColumn(
                name: "LineTotal",
                table: "PurchaseOrderLines",
                newName: "SubTotal");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "SalesOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SalesOrderLines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PurchaseOrderLines",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
