using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePrescreening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HasReceivedBloodTransfusionWithinPastYear",
                table: "HealthPreScreenings",
                newName: "HasDentalProcedureInPastWeek");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HasDentalProcedureInPastWeek",
                table: "HealthPreScreenings",
                newName: "HasReceivedBloodTransfusionWithinPastYear");
        }
    }
}
