using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodDemandTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BloodDemandId",
                table: "IssuanceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BloodDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BloodTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    RequesterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestedUnits = table.Column<int>(type: "int", nullable: false),
                    IssuedUnits = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodDemands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodDemands_BloodTypes_BloodTypeId",
                        column: x => x.BloodTypeId,
                        principalTable: "BloodTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuanceRecords_BloodDemandId",
                table: "IssuanceRecords",
                column: "BloodDemandId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodDemands_BloodTypeId",
                table: "BloodDemands",
                column: "BloodTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_IssuanceRecords_BloodDemands_BloodDemandId",
                table: "IssuanceRecords",
                column: "BloodDemandId",
                principalTable: "BloodDemands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssuanceRecords_BloodDemands_BloodDemandId",
                table: "IssuanceRecords");

            migrationBuilder.DropTable(
                name: "BloodDemands");

            migrationBuilder.DropIndex(
                name: "IX_IssuanceRecords_BloodDemandId",
                table: "IssuanceRecords");

            migrationBuilder.DropColumn(
                name: "BloodDemandId",
                table: "IssuanceRecords");
        }
    }
}
