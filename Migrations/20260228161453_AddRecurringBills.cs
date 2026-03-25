using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack_Pro.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "Bills",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RecurringFrequency",
                table: "Bills",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "RecurringFrequency",
                table: "Bills");
        }
    }
}
