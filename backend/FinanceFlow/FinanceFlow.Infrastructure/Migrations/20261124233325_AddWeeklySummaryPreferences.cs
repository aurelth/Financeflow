using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklySummaryPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WeeklySummaryEmailEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WeeklySummaryEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklySummaryEmailEnabled",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "WeeklySummaryEnabled",
                table: "UserNotificationPreferences");
        }
    }
}
