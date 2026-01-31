using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShivFurnitureERP.Migrations
{
    /// <inheritdoc />
    public partial class AutoAnalyticalRuleEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoAnalyticalModels",
                columns: table => new
                {
                    ModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    PartnerTagId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    ProductCategoryId = table.Column<int>(type: "int", nullable: true),
                    AnalyticalAccountId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoAnalyticalModels", x => x.ModelId);
                    table.ForeignKey(
                        name: "FK_AutoAnalyticalModels_AnalyticalAccounts_AnalyticalAccountId",
                        column: x => x.AnalyticalAccountId,
                        principalTable: "AnalyticalAccounts",
                        principalColumn: "AnalyticalAccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutoAnalyticalModels_Contacts_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoAnalyticalModels_ProductCategories_ProductCategoryId",
                        column: x => x.ProductCategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "ProductCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoAnalyticalModels_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoAnalyticalModels_Tags_PartnerTagId",
                        column: x => x.PartnerTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoAnalyticalModels_AnalyticalAccountId",
                table: "AutoAnalyticalModels",
                column: "AnalyticalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoAnalyticalModels_PartnerId_PartnerTagId_ProductId_ProductCategoryId_Status",
                table: "AutoAnalyticalModels",
                columns: new[] { "PartnerId", "PartnerTagId", "ProductId", "ProductCategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AutoAnalyticalModels_PartnerTagId",
                table: "AutoAnalyticalModels",
                column: "PartnerTagId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoAnalyticalModels_ProductCategoryId",
                table: "AutoAnalyticalModels",
                column: "ProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoAnalyticalModels_ProductId",
                table: "AutoAnalyticalModels",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoAnalyticalModels");
        }
    }
}
