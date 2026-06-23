using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultBloodStockThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BloodStockThresholds",
                columns: new[] { "Id", "BloodTypeId", "CriticalThreshold", "LowThreshold" },
                values: new object[,]
                {
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000001"), (byte)1, 5, 10 },
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000002"), (byte)2, 4, 8 },
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000003"), (byte)3, 6, 12 },
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000004"), (byte)4, 5, 10 },
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000005"), (byte)5, 2, 5 },
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000006"), (byte)6, 2, 5 },
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000007"), (byte)7, 7, 15 },
                    { new Guid("f0c1b2a3-9876-4321-b123-abcdef000008"), (byte)8, 5, 10 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000001"));

            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000002"));

            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000003"));

            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000004"));

            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000005"));

            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000006"));

            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000007"));

            migrationBuilder.DeleteData(
                table: "BloodStockThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0c1b2a3-9876-4321-b123-abcdef000008"));
        }
    }
}
