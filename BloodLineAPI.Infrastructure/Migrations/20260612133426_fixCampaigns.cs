using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivationJobId",
                table: "DonationCenters");

            migrationBuilder.DropColumn(
                name: "CompletionJobId",
                table: "DonationCenters");

            migrationBuilder.AddColumn<string>(
                name: "ScheduledJobIds",
                table: "DonationCenters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DonationCenters",
                keyColumn: "Id",
                keyValue: new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"),
                column: "ScheduledJobIds",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledJobIds",
                table: "DonationCenters");

            migrationBuilder.AddColumn<string>(
                name: "ActivationJobId",
                table: "DonationCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionJobId",
                table: "DonationCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DonationCenters",
                keyColumn: "Id",
                keyValue: new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"),
                columns: new[] { "ActivationJobId", "CompletionJobId" },
                values: new object[] { null, null });
        }
    }
}
