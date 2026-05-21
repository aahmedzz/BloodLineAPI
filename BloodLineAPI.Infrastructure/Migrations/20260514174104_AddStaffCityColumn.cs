using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffCityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Staff",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("1f7e02df-8a39-44d5-ab85-9b87f252601c"), "20a435e9-1e42-4e7c-8d4f-0181c52d5845", "InventoryManager", "INVENTORYMANAGER" },
                    { new Guid("7b9a5f78-1cf5-4e3a-967b-1a938c23ab7c"), "eb09d12f-1e4a-4aee-8fca-caac79eb4d9b", "LabDoctor", "LABDOCTOR" },
                    { new Guid("d40d9d40-4252-4a7f-ae68-3e9862d512a8"), "1c6958c7-2806-4cec-941d-b63689dc93e3", "Admin", "ADMIN" },
                    { new Guid("e35b7194-22b6-4b2a-8924-f7b5fae5f75e"), "91b5145a-e3db-412e-b95a-cd6d29e00fbb", "Doctor", "DOCTOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("1f7e02df-8a39-44d5-ab85-9b87f252601c"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("7b9a5f78-1cf5-4e3a-967b-1a938c23ab7c"));


            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("d40d9d40-4252-4a7f-ae68-3e9862d512a8"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("e35b7194-22b6-4b2a-8924-f7b5fae5f75e"));

            migrationBuilder.DropColumn(
                name: "City",
                table: "Staff");
        }
    }
}
