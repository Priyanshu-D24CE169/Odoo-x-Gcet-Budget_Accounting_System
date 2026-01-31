using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShivFurnitureERP.Migrations
{
    /// <inheritdoc />
    public partial class VendorBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorBills",
                columns: table => new
                {
                    VendorBillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: true),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBills", x => x.VendorBillId);
                    table.ForeignKey(
                        name: "FK_VendorBills_Contacts_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorBills_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VendorBillLines",
                columns: table => new
                {
                    VendorBillLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorBillId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnalyticalAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBillLines", x => x.VendorBillLineId);
                    table.ForeignKey(
                        name: "FK_VendorBillLines_AnalyticalAccounts_AnalyticalAccountId",
                        column: x => x.AnalyticalAccountId,
                        principalTable: "AnalyticalAccounts",
                        principalColumn: "AnalyticalAccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorBillLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorBillLines_VendorBills_VendorBillId",
                        column: x => x.VendorBillId,
                        principalTable: "VendorBills",
                        principalColumn: "VendorBillId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillLines_AnalyticalAccountId",
                table: "VendorBillLines",
                column: "AnalyticalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillLines_ProductId",
                table: "VendorBillLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillLines_VendorBillId",
                table: "VendorBillLines",
                column: "VendorBillId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_BillNumber",
                table: "VendorBills",
                column: "BillNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_PurchaseOrderId",
                table: "VendorBills",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_VendorId",
                table: "VendorBills",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorBillLines");

            migrationBuilder.DropTable(
                name: "VendorBills");
        }
    }
}
