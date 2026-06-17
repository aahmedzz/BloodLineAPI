using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DonationAppointmentId",
                table: "MedicalScreenings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Donors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Eligible");

            migrationBuilder.AddColumn<int>(
                name: "DonationNumber",
                table: "DonationAppointments",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "DonationStatus",
                table: "DonationAppointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalScreeningId",
                table: "DonationAppointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SentToLab",
                table: "DonationAppointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DonationCode",
                table: "DonationAppointments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                computedColumnSql: "'DTN-' + CAST(YEAR([CreatedAt]) AS VARCHAR(4)) + '-' + CASE WHEN [DonationNumber] < 10000 THEN RIGHT('0000' + CAST([DonationNumber] AS VARCHAR(10)), 4) ELSE CAST([DonationNumber] AS VARCHAR(10)) END",
                stored: false);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalScreenings_DonationAppointmentId",
                table: "MedicalScreenings",
                column: "DonationAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationAppointments_DonationCode",
                table: "DonationAppointments",
                column: "DonationCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalScreenings_DonationAppointments_DonationAppointmentId",
                table: "MedicalScreenings",
                column: "DonationAppointmentId",
                principalTable: "DonationAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalScreenings_DonationAppointments_DonationAppointmentId",
                table: "MedicalScreenings");

            migrationBuilder.DropIndex(
                name: "IX_MedicalScreenings_DonationAppointmentId",
                table: "MedicalScreenings");

            migrationBuilder.DropIndex(
                name: "IX_DonationAppointments_DonationCode",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "DonationCode",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "DonationAppointmentId",
                table: "MedicalScreenings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "DonationNumber",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "DonationStatus",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "MedicalScreeningId",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "SentToLab",
                table: "DonationAppointments");
        }
    }
}
