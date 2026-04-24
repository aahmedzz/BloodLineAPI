using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailableDonationTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupportedDonationTypes",
                table: "DonationCenters",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "DonationCenters",
                keyColumn: "Id",
                keyValue: new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"),
                column: "SupportedDonationTypes",
                value: "WholeBlood,Plasma,Platelets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupportedDonationTypes",
                table: "DonationCenters");
        }
    }
}
