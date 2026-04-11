using BloodLineAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260411124500_RenameCityToGovernorateAddAreaAndFullAddress")]
    public partial class RenameCityToGovernorateAddAreaAndFullAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "City",
                table: "Donors",
                newName: "Governorate");

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Donors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Donors SET Area = Address WHERE Area = ''; ");
            migrationBuilder.Sql("UPDATE Donors SET Address = CONCAT_WS(', ', NULLIF(Area, ''), NULLIF(District, ''), NULLIF(Governorate, '')); ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "Donors");

            migrationBuilder.RenameColumn(
                name: "Governorate",
                table: "Donors",
                newName: "City");
        }
    }
}
