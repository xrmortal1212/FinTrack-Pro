using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack_Pro.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeToDebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Debts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Debts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Debts");
        }
    }
}
