using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivationJobId",
                table: "DonationCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CampaignNumber",
                table: "DonationCenters",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "CompletionJobId",
                table: "DonationCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "DonationCenters",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "DonationCenters",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecurrenceEnabled",
                table: "DonationCenters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndDate",
                table: "DonationCenters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceGroupId",
                table: "DonationCenters",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceType",
                table: "DonationCenters",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceWeekDays",
                table: "DonationCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetDonors",
                table: "DonationCenters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CampaignCode",
                table: "DonationCenters",
                type: "nvarchar(max)",
                nullable: true,
                computedColumnSql: "'CAM-' + RIGHT('000' + CAST(CampaignNumber AS VARCHAR(10)), 3)",
                stored: true);

            migrationBuilder.UpdateData(
                table: "DonationCenters",
                keyColumn: "Id",
                keyValue: new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"),
                columns: new[] { "ActivationJobId", "CompletionJobId", "CreatedById", "CreatedByName", "RecurrenceEnabled", "RecurrenceEndDate", "RecurrenceGroupId", "RecurrenceType", "RecurrenceWeekDays", "TargetDonors" },
                values: new object[] { null, null, null, null, false, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignCode",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "ActivationJobId",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "CampaignNumber",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "CompletionJobId",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "RecurrenceEnabled",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "RecurrenceGroupId",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "RecurrenceType",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "RecurrenceWeekDays",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "TargetDonors",
                table: "DonationCenters");
        }
    }
}
