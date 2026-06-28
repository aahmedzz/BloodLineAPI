using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationAndUrgentAppealRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("2ef1880e-ca8b-4383-8584-3b31f1fb9448"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("7db611c5-b017-48d3-84fb-2273d61938df"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("db1d6774-bf5f-4fdd-aea3-b3865e6a546d"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e98fa758-1395-4ca1-a17c-51b965ef8f77"));

            migrationBuilder.AddColumn<Guid>(
                name: "UrgentBloodAppealId",
                table: "DonationAppointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("89e198c4-d715-4cf6-a85f-7696159f423a"),
                column: "BonusPoints",
                value: 500);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("c80d19d2-63d9-4fb6-bf77-fa8f253f50b4"),
                columns: new[] { "BadgeDescription", "BonusPoints" },
                values: new object[] { "Awarded after 11 completed donations.", 1000 });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("cc39fc72-7f5f-4d17-9bfd-9a2f3f5b8db1"),
                column: "BonusPoints",
                value: 300);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e3f8c38b-3858-45f0-b571-c7ec736dbfee"),
                column: "BonusPoints",
                value: 800);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f6ee9496-e7fb-43c3-8275-ec5f35cf01a1"),
                column: "BonusPoints",
                value: 100);

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "BadgeDescription", "BadgeKey", "BadgeName", "BadgeNameAr", "BadgeType", "BonusPoints", "IconUrl" },
                values: new object[,]
                {
                    { new Guid("02cdeef5-2b0e-473d-88b6-9de0237e1a07"), "Awarded for donating during winter (December, January, February).", "winter_guard", "Winter Guard", "حارس الشتاء", "Action", 200, "badges/winter_guard.png" },
                    { new Guid("12cdeef5-2b0e-473d-88b6-9de0237e1a08"), "Awarded for donating during Eid holidays.", "eid_savior", "Eid Savior", "منقذ العيد", "Action", 250, "badges/eid_savior.png" },
                    { new Guid("a2cdeef5-2b0e-473d-88b6-9de0237e1a01"), "Awarded for donating platelets.", "platelet_guardian", "Platelet Guardian", "حارس الصفائح", "Action", 200, "badges/platelet_guardian.png" },
                    { new Guid("b2cdeef5-2b0e-473d-88b6-9de0237e1a02"), "Awarded for donating plasma.", "yellow_gold", "Yellow Gold", "الذهب الأصفر", "Action", 200, "badges/yellow_gold.png" },
                    { new Guid("c2cdeef5-2b0e-473d-88b6-9de0237e1a03"), "Awarded for donating whole blood, platelets, and plasma.", "triple_giver", "The Triple Giver", "المعطي الثلاثي", "Action", 600, "badges/triple_giver.png" },
                    { new Guid("d2cdeef5-2b0e-473d-88b6-9de0237e1a04"), "Awarded for donating in a district other than your home district.", "traveler_lifesaver", "Traveler Lifesaver", "المنقذ المسافر", "Action", 150, "badges/traveler_lifesaver.png" },
                    { new Guid("e2cdeef5-2b0e-473d-88b6-9de0237e1a05"), "Awarded for donating on a Friday or Saturday.", "weekend_hero", "Weekend Hero", "بطل عطلة نهاية الأسبوع", "Action", 100, "badges/weekend_hero.png" },
                    { new Guid("f2cdeef5-2b0e-473d-88b6-9de0237e1a06"), "Awarded for donating during the holy month of Ramadan.", "ramadan_light", "The Ramadan Light", "نور رمضان", "Action", 300, "badges/ramadan_light.png" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationAppointments_UrgentBloodAppealId",
                table: "DonationAppointments",
                column: "UrgentBloodAppealId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationAppointments_UrgentBloodAppeals_UrgentBloodAppealId",
                table: "DonationAppointments",
                column: "UrgentBloodAppealId",
                principalTable: "UrgentBloodAppeals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationAppointments_UrgentBloodAppeals_UrgentBloodAppealId",
                table: "DonationAppointments");

            migrationBuilder.DropIndex(
                name: "IX_DonationAppointments_UrgentBloodAppealId",
                table: "DonationAppointments");

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("02cdeef5-2b0e-473d-88b6-9de0237e1a07"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("12cdeef5-2b0e-473d-88b6-9de0237e1a08"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("a2cdeef5-2b0e-473d-88b6-9de0237e1a01"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("b2cdeef5-2b0e-473d-88b6-9de0237e1a02"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("c2cdeef5-2b0e-473d-88b6-9de0237e1a03"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("d2cdeef5-2b0e-473d-88b6-9de0237e1a04"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e2cdeef5-2b0e-473d-88b6-9de0237e1a05"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f2cdeef5-2b0e-473d-88b6-9de0237e1a06"));

            migrationBuilder.DropColumn(
                name: "UrgentBloodAppealId",
                table: "DonationAppointments");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("89e198c4-d715-4cf6-a85f-7696159f423a"),
                column: "BonusPoints",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("c80d19d2-63d9-4fb6-bf77-fa8f253f50b4"),
                columns: new[] { "BadgeDescription", "BonusPoints" },
                values: new object[] { "Awarded after 20 completed donations.", 300 });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("cc39fc72-7f5f-4d17-9bfd-9a2f3f5b8db1"),
                column: "BonusPoints",
                value: 75);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e3f8c38b-3858-45f0-b571-c7ec736dbfee"),
                column: "BonusPoints",
                value: 150);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f6ee9496-e7fb-43c3-8275-ec5f35cf01a1"),
                column: "BonusPoints",
                value: 50);

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "BadgeDescription", "BadgeKey", "BadgeName", "BadgeNameAr", "BadgeType", "BonusPoints", "IconUrl" },
                values: new object[,]
                {
                    { new Guid("2ef1880e-ca8b-4383-8584-3b31f1fb9448"), "Awarded for the first emergency donation.", "responder", "Responder", "المستجيب", "Action", 100, "badges/responder.png" },
                    { new Guid("7db611c5-b017-48d3-84fb-2273d61938df"), "Awarded after sharing 20 urgent requests.", "ambassador", "Ambassador", "السفير", "Action", 80, "badges/ambassador.png" },
                    { new Guid("db1d6774-bf5f-4fdd-aea3-b3865e6a546d"), "Awarded for donors with rare blood type O- or AB-.", "golden_blood", "Golden Blood", "الدم الذهبي", "Action", 100, "badges/golden_blood.png" },
                    { new Guid("e98fa758-1395-4ca1-a17c-51b965ef8f77"), "Awarded for donating after midnight.", "night_owl", "Night Owl", "بومة الليل", "Action", 60, "badges/night_owl.png" }
                });
        }
    }
}
