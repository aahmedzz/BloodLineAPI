using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissedDonorColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonthlyPoints",
                table: "Donors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDonationCount",
                table: "Donors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TotalPoints",
                table: "Donors",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyPoints",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "TotalDonationCount",
                table: "Donors");

            migrationBuilder.AlterColumn<int>(
                name: "TotalPoints",
                table: "Donors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);
        }
    }
}
