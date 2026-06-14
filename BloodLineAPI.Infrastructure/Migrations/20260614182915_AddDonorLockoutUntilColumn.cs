using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorLockoutUntilColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntil",
                table: "Donors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE d
                SET d.LockoutUntil = ms.LockoutUntil
                FROM Donors d
                INNER JOIN (
                    SELECT DonorId, LockoutUntil,
                           ROW_NUMBER() OVER (PARTITION BY DonorId ORDER BY ScreeningDate DESC) AS rn
                    FROM MedicalScreenings
                    WHERE IsEligible = 0 AND LockoutUntil IS NOT NULL
                ) ms ON d.Id = ms.DonorId AND ms.rn = 1
                WHERE d.Status IN ('Deferred')
                  AND ms.LockoutUntil > GETUTCDATE()
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                table: "Donors");
        }
    }
}
