using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAppointmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add HealthPreScreenings table if it doesn't exist
            migrationBuilder.CreateTable(
                name: "HealthPreScreenings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HasChronicDisease = table.Column<bool>(type: "bit", nullable: false),
                    HasRecentSurgery = table.Column<bool>(type: "bit", nullable: false),
                    IsTakingMedication = table.Column<bool>(type: "bit", nullable: false),
                    HasRecentTattooOrPiercing = table.Column<bool>(type: "bit", nullable: false),
                    HasRecentInfection = table.Column<bool>(type: "bit", nullable: false),
                    IsPregnantOrBreastfeeding = table.Column<bool>(type: "bit", nullable: false),
                    HasBleedingDisorder = table.Column<bool>(type: "bit", nullable: false),
                    HasRecentVaccination = table.Column<bool>(type: "bit", nullable: false),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    ScreenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthPreScreenings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthPreScreenings_Donors_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Donors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Add PointTransactions table
            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointTransactions_Donors_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Donors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add missing columns to DonationAppointments
            migrationBuilder.AddColumn<Guid>(
                name: "HealthPreScreeningId",
                table: "DonationAppointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "DonationAppointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "DonationAppointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DonationAppointments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Update Status and DonationType lengths if needed (InitialCreate was 100 and max)
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DonationAppointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DonationType",
                table: "DonationAppointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            // Create Indexes
            migrationBuilder.CreateIndex(
                name: "IX_DonationAppointments_HealthPreScreeningId",
                table: "DonationAppointments",
                column: "HealthPreScreeningId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthPreScreenings_DonorId",
                table: "HealthPreScreenings",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_DonorId",
                table: "PointTransactions",
                column: "DonorId");

            // Add Foreign Key
            migrationBuilder.AddForeignKey(
                name: "FK_DonationAppointments_HealthPreScreenings_HealthPreScreeningId",
                table: "DonationAppointments",
                column: "HealthPreScreeningId",
                principalTable: "HealthPreScreenings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationAppointments_HealthPreScreenings_HealthPreScreeningId",
                table: "DonationAppointments");

            migrationBuilder.DropTable(
                name: "PointTransactions");

            migrationBuilder.DropTable(
                name: "HealthPreScreenings");

            migrationBuilder.DropIndex(
                name: "IX_DonationAppointments_HealthPreScreeningId",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "HealthPreScreeningId",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DonationAppointments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DonationAppointments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DonationType",
                table: "DonationAppointments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
