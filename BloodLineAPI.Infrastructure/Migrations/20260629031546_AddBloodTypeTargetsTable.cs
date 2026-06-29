using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodTypeTargetsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloodTypeTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonationCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TargetCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodTypeTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodTypeTargets_DonationCenters_DonationCenterId",
                        column: x => x.DonationCenterId,
                        principalTable: "DonationCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BloodTypeTargets",
                columns: new[] { "Id", "BloodType", "DonationCenterId", "TargetCount" },
                values: new object[,]
                {
                    { new Guid("b5b4d5b7-f001-4a92-8b0a-2fc73f6c0001"), "A+", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 40 },
                    { new Guid("b5b4d5b7-f002-4a92-8b0a-2fc73f6c0002"), "A-", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 10 },
                    { new Guid("b5b4d5b7-f003-4a92-8b0a-2fc73f6c0003"), "B+", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 50 },
                    { new Guid("b5b4d5b7-f004-4a92-8b0a-2fc73f6c0004"), "B-", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 10 },
                    { new Guid("b5b4d5b7-f005-4a92-8b0a-2fc73f6c0005"), "AB+", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 20 },
                    { new Guid("b5b4d5b7-f006-4a92-8b0a-2fc73f6c0006"), "AB-", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 5 },
                    { new Guid("b5b4d5b7-f007-4a92-8b0a-2fc73f6c0007"), "O+", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 60 },
                    { new Guid("b5b4d5b7-f008-4a92-8b0a-2fc73f6c0008"), "O-", new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), 15 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloodTypeTargets_DonationCenterId_BloodType",
                table: "BloodTypeTargets",
                columns: new[] { "DonationCenterId", "BloodType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloodTypeTargets");
        }
    }
}
