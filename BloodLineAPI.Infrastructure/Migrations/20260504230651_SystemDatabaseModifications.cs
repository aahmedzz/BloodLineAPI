using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SystemDatabaseModifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "DiscardRecords");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Staff",
                newName: "ThirdName");

            migrationBuilder.RenameColumn(
                name: "ChronicDiseases",
                table: "MedicalScreenings",
                newName: "HasChronicDiseases");

            migrationBuilder.RenameColumn(
                name: "BloodPressure",
                table: "MedicalScreenings",
                newName: "SystolicBP");

            migrationBuilder.RenameColumn(
                name: "HepatitisResult",
                table: "BloodTestResults",
                newName: "SyphilisResult");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Staff",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FourthName",
                table: "Staff",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Staff",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondName",
                table: "Staff",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChronicDiseaseDetails",
                table: "MedicalScreenings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiastolicBP",
                table: "MedicalScreenings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PulseRate",
                table: "MedicalScreenings",
                type: "decimal(5,1)",
                precision: 5,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "MedicalScreenings",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DonorNumber",
                table: "Donors",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Donors",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Donors",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "DonationAppointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReasonCategory",
                table: "DiscardRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReasonDetails",
                table: "DiscardRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HepatitisBResult",
                table: "BloodTestResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ConfirmedBloodTypeId",
                table: "BloodTestResults",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HepatitisCResult",
                table: "BloodTestResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "BloodTestResults",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BagType",
                table: "BloodBags",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DonorCode",
                table: "Donors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                computedColumnSql: "'DNR-' + CAST(YEAR([CreatedAt]) AS VARCHAR(4)) + '-' + CASE WHEN [DonorNumber] < 10000 THEN RIGHT('0000' + CAST([DonorNumber] AS VARCHAR(10)), 4) ELSE CAST([DonorNumber] AS VARCHAR(10)) END",
                stored: false);

            migrationBuilder.CreateTable(
                name: "BloodStockThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodTypeId = table.Column<byte>(type: "tinyint", nullable: true),
                    LowThreshold = table.Column<int>(type: "int", nullable: false),
                    CriticalThreshold = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodStockThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodStockThresholds_BloodTypes_BloodTypeId",
                        column: x => x.BloodTypeId,
                        principalTable: "BloodTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Donors_DonorCode",
                table: "Donors",
                column: "DonorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BloodTestResults_ConfirmedBloodTypeId",
                table: "BloodTestResults",
                column: "ConfirmedBloodTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodStockThresholds_BloodTypeId",
                table: "BloodStockThresholds",
                column: "BloodTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BloodTestResults_BloodTypes_ConfirmedBloodTypeId",
                table: "BloodTestResults",
                column: "ConfirmedBloodTypeId",
                principalTable: "BloodTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BloodTestResults_BloodTypes_ConfirmedBloodTypeId",
                table: "BloodTestResults");

            migrationBuilder.DropTable(
                name: "BloodStockThresholds");

            migrationBuilder.DropIndex(
                name: "IX_Donors_DonorCode",
                table: "Donors");

            migrationBuilder.DropIndex(
                name: "IX_BloodTestResults_ConfirmedBloodTypeId",
                table: "BloodTestResults");

            migrationBuilder.DropColumn(
                name: "DonorCode",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "FourthName",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "SecondName",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "ChronicDiseaseDetails",
                table: "MedicalScreenings");

            migrationBuilder.DropColumn(
                name: "DiastolicBP",
                table: "MedicalScreenings");

            migrationBuilder.DropColumn(
                name: "PulseRate",
                table: "MedicalScreenings");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "MedicalScreenings");

            migrationBuilder.DropColumn(
                name: "DonorNumber",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "DonationAppointments");

            migrationBuilder.DropColumn(
                name: "ReasonCategory",
                table: "DiscardRecords");

            migrationBuilder.DropColumn(
                name: "ReasonDetails",
                table: "DiscardRecords");

            migrationBuilder.DropColumn(
                name: "ConfirmedBloodTypeId",
                table: "BloodTestResults");

            migrationBuilder.DropColumn(
                name: "HepatitisCResult",
                table: "BloodTestResults");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "BloodTestResults");

            migrationBuilder.DropColumn(
                name: "BagType",
                table: "BloodBags");

            migrationBuilder.RenameColumn(
                name: "ThirdName",
                table: "Staff",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "SystolicBP",
                table: "MedicalScreenings",
                newName: "BloodPressure");

            migrationBuilder.RenameColumn(
                name: "HasChronicDiseases",
                table: "MedicalScreenings",
                newName: "ChronicDiseases");

            migrationBuilder.RenameColumn(
                name: "SyphilisResult",
                table: "BloodTestResults",
                newName: "HepatitisResult");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "DiscardRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "HepatitisBResult",
                table: "BloodTestResults",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
