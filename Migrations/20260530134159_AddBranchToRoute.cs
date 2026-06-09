using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PUBReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Branch_ID",
                table: "Routes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    SaleID = table.Column<int>(name: "Sale_ID", type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaleDate = table.Column<DateTime>(name: "Sale_Date", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.SaleID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Branch_ID",
                table: "Routes",
                column: "Branch_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Branches_Branch_ID",
                table: "Routes",
                column: "Branch_ID",
                principalTable: "Branches",
                principalColumn: "Branch_ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Branches_Branch_ID",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Branch_ID",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "Branch_ID",
                table: "Routes");
        }
    }
}
