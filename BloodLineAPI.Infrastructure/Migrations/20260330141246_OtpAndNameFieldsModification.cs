using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OtpAndNameFieldsModification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Donors",
                newName: "ThirdName");

            migrationBuilder.AddColumn<string>(
                name: "FourthName",
                table: "Donors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRegistrationCompleted",
                table: "Donors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecondName",
                table: "Donors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "Donors",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationOtpCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationOtpExpiryTime",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FourthName",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "IsRegistrationCompleted",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "SecondName",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "RegistrationOtpCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationOtpExpiryTime",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "ThirdName",
                table: "Donors",
                newName: "LastName");
        }
    }
}
