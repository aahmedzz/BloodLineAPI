using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "ActionPayload",
                table: "Notifications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" },
                filter: "[IsRead] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_SentDate_Id",
                table: "Notifications",
                columns: new[] { "UserId", "SentDate", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_SentDate_Id",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActionPayload",
                table: "Notifications");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }
    }
}
