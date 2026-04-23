using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissedBadgeAndScreeningColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Badges table
            migrationBuilder.AddColumn<string>(
                name: "BadgeKey",
                table: "Badges",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BadgeNameAr",
                table: "Badges",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BadgeType",
                table: "Badges",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Milestone");

            migrationBuilder.AddColumn<int>(
                name: "BonusPoints",
                table: "Badges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Badges_BadgeKey",
                table: "Badges",
                column: "BadgeKey",
                unique: true);

            migrationBuilder.DropColumn(
                name: "RequiredPoints",
                table: "Badges");

            // DonorBadges table
            migrationBuilder.Sql("""
                IF COL_LENGTH('DonorBadges', 'AcquiredDate') IS NOT NULL
                   AND COL_LENGTH('DonorBadges', 'EarnedDate') IS NULL
                BEGIN
                    EXEC sp_rename N'[DonorBadges].[AcquiredDate]', N'EarnedDate', 'COLUMN';
                END
                """);

            // MedicalScreenings table
            migrationBuilder.AddColumn<Guid>(
                name: "DonorId",
                table: "MedicalScreenings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntil",
                table: "MedicalScreenings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalScreenings_DonorId",
                table: "MedicalScreenings",
                column: "DonorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalScreenings_Donors_DonorId",
                table: "MedicalScreenings",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalScreenings_Donors_DonorId",
                table: "MedicalScreenings");

            migrationBuilder.DropIndex(
                name: "IX_MedicalScreenings_DonorId",
                table: "MedicalScreenings");

            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                table: "MedicalScreenings");

            migrationBuilder.DropColumn(
                name: "DonorId",
                table: "MedicalScreenings");

            migrationBuilder.Sql("""
                IF COL_LENGTH('DonorBadges', 'EarnedDate') IS NOT NULL
                   AND COL_LENGTH('DonorBadges', 'AcquiredDate') IS NULL
                BEGIN
                    EXEC sp_rename N'[DonorBadges].[EarnedDate]', N'AcquiredDate', 'COLUMN';
                END
                """);

            migrationBuilder.AddColumn<int>(
                name: "RequiredPoints",
                table: "Badges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_Badges_BadgeKey",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "BonusPoints",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "BadgeType",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "BadgeNameAr",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "BadgeKey",
                table: "Badges");
        }
    }
}
