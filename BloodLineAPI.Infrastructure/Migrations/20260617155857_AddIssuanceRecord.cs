using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuanceRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssuanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodBagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuanceRecords_BloodBags_BloodBagId",
                        column: x => x.BloodBagId,
                        principalTable: "BloodBags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssuanceRecords_Staff_IssuedByStaffId",
                        column: x => x.IssuedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuanceRecords_BloodBagId",
                table: "IssuanceRecords",
                column: "BloodBagId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssuanceRecords_IssuedByStaffId",
                table: "IssuanceRecords",
                column: "IssuedByStaffId");

            // Migrate existing enum string values
            migrationBuilder.Sql("UPDATE BloodBags SET Status = 'Disposed' WHERE Status = 'Discarded'");
            migrationBuilder.Sql("UPDATE BloodBags SET Status = 'Issued' WHERE Status = 'Exported'");
            migrationBuilder.Sql("UPDATE DiscardRecords SET ReasonCategory = 'FailedScreening' WHERE ReasonCategory = 'LabRejected'");
            migrationBuilder.Sql("UPDATE DiscardRecords SET ReasonCategory = 'DamagedStorage' WHERE ReasonCategory = 'PhysicalDamage'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET PreviousStatus = 'Disposed' WHERE PreviousStatus = 'Discarded'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET NewStatus = 'Disposed' WHERE NewStatus = 'Discarded'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET PreviousStatus = 'Issued' WHERE PreviousStatus = 'Exported'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET NewStatus = 'Issued' WHERE NewStatus = 'Exported'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback migrated enum string values
            migrationBuilder.Sql("UPDATE BloodBags SET Status = 'Discarded' WHERE Status = 'Disposed'");
            migrationBuilder.Sql("UPDATE BloodBags SET Status = 'Exported' WHERE Status = 'Issued'");
            migrationBuilder.Sql("UPDATE DiscardRecords SET ReasonCategory = 'LabRejected' WHERE ReasonCategory = 'FailedScreening'");
            migrationBuilder.Sql("UPDATE DiscardRecords SET ReasonCategory = 'PhysicalDamage' WHERE ReasonCategory = 'DamagedStorage'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET PreviousStatus = 'Discarded' WHERE PreviousStatus = 'Disposed'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET NewStatus = 'Discarded' WHERE NewStatus = 'Disposed'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET PreviousStatus = 'Exported' WHERE PreviousStatus = 'Issued'");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET NewStatus = 'Exported' WHERE NewStatus = 'Issued'");

            migrationBuilder.DropTable(
                name: "IssuanceRecords");
        }
    }
}
