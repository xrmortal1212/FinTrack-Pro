using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack_Pro.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToInvestment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Investments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "Investments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "Investments");
        }
    }
}
