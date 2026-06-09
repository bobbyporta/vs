using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PUBReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddArchivedToBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Is_Archived",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Is_Archived",
                table: "Branches");
        }
    }
}
