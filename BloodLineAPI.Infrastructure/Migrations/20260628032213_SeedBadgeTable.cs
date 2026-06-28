using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBadgeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeDescriptionAr",
                table: "Badges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("02cdeef5-2b0e-473d-88b6-9de0237e1a07"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "Warming hearts in the cold! Your winter donation helps keep blood banks warm and ready during seasonal shortages.", "دفء القلوب في برد الشتاء! يساعد تبرعك الشتوي في بقاء بنوك الدم مستعدة خلال فترات نقص المخزون الموسمية." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("12cdeef5-2b0e-473d-88b6-9de0237e1a08"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "Spreading joy in Eid! Your generous contribution ensures that festive times remain safe for patients undergoing emergency care.", "ناشر الفرحة في العيد! يضمن تبرعك السخي بقاء أوقات الأعياد آمنة للمرضى الذين يحتاجون لرعاية طارئة." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("89e198c4-d715-4cf6-a85f-7696159f423a"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "A title for the brave! Your consistent contributions have made you a real-life hero in the eyes of those you've saved.", "لقب للشجعان! تبرعاتك المستمرة جعلت منك بطلاً حقيقياً في عيون أولئك الذين أنقذتهم." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("a2cdeef5-2b0e-473d-88b6-9de0237e1a01"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "Defender of the vulnerable! Your dedicated platelet donations provide strength and hope to those fighting critical conditions.", "مدافع عن الفئات الأكثر ضعفاً! تبرعك بالصفائح الدموية يمنح القوة والأمل للمرضى في الحالات الحرجة." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("b2cdeef5-2b0e-473d-88b6-9de0237e1a02"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "Liquid gold giver! Your plasma donation helps create life-saving therapies for patients in intensive care.", "معطي الذهب السائل! يساعد تبرعك بالبلازما في توفير العلاجات المنقذة للحياة لمرضى العناية المركزة." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("c2cdeef5-2b0e-473d-88b6-9de0237e1a03"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "The ultimate trifecta! You have shown outstanding dedication by donating whole blood, platelets, and plasma.", "العطاء الثلاثي المتكامل! لقد أظهرت تفانياً استثنائياً بالتبرع بالدم الكامل، والصفائح الدموية، والبلازما." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("c80d19d2-63d9-4fb6-bf77-fa8f253f50b4"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "The ultimate elite! You are now a legendary savior in your community, leaving a legacy of hope and saved lives.", "النخبة المطلقة! أنت الآن منقذ أسطوري في مجتمعك، تاركاً إرثاً من الأمل والأرواح التي تم إنقاذها." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("cc39fc72-7f5f-4d17-9bfd-9a2f3f5b8db1"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "A true community pillar! By donating 3 times, you have helped sustain the lives of more than 9 people.", "ركيزة حقيقية للمجتمع! بتبرعك 3 مرات، ساعدت في دعم حياة أكثر من 9 أشخاص." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("d2cdeef5-2b0e-473d-88b6-9de0237e1a04"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "A savior without borders! You went the extra mile by donating outside your home district to support another community.", "منقذ بلا حدود! لقد قطعت مسافة إضافية بالتبرع خارج منطقتك السكنية لدعم مجتمع آخر بحاجة للدم." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e2cdeef5-2b0e-473d-88b6-9de0237e1a05"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "Guardian of the weekend! You chose to spend your weekend giving back and ensuring blood is available when it is needed most.", "بطل عطلة نهاية الأسبوع! لقد اخترت قضاء عطلتك في العطاء وتأمين مخزون الدم للحالات الطارئة." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e3f8c38b-3858-45f0-b571-c7ec736dbfee"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "Your generosity knows no bounds! With 10 donations, you have become a vital protector of countless lives.", "كرمك ليس له حدود! مع 10 تبرعات، أصبحت حامياً حيوياً لحياة لا حصر لها." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f2cdeef5-2b0e-473d-88b6-9de0237e1a06"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "Shedding light in the holy month! Your donation during Ramadan brings blessings and hope to families in need.", "نور يضيء في الشهر الفضيل! تبرعك بالدم خلال شهر رمضان يمنح البركة والأمل للعائلات المحتاجة." });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f6ee9496-e7fb-43c3-8275-ec5f35cf01a1"),
                columns: new[] { "BadgeDescription", "BadgeDescriptionAr" },
                values: new object[] { "The journey begins! You've earned this title by giving the gift of life and taking your first step toward saving others.", "تبدأ الرحلة! لقد حصلت على هذا اللقب بتقديمك هدية الحياة واتخاذ خطوتك الأولى نحو إنقاذ الآخرين." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeDescriptionAr",
                table: "Badges");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("02cdeef5-2b0e-473d-88b6-9de0237e1a07"),
                column: "BadgeDescription",
                value: "Awarded for donating during winter (December, January, February).");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("12cdeef5-2b0e-473d-88b6-9de0237e1a08"),
                column: "BadgeDescription",
                value: "Awarded for donating during Eid holidays.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("89e198c4-d715-4cf6-a85f-7696159f423a"),
                column: "BadgeDescription",
                value: "Awarded after 5 completed donations.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("a2cdeef5-2b0e-473d-88b6-9de0237e1a01"),
                column: "BadgeDescription",
                value: "Awarded for donating platelets.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("b2cdeef5-2b0e-473d-88b6-9de0237e1a02"),
                column: "BadgeDescription",
                value: "Awarded for donating plasma.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("c2cdeef5-2b0e-473d-88b6-9de0237e1a03"),
                column: "BadgeDescription",
                value: "Awarded for donating whole blood, platelets, and plasma.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("c80d19d2-63d9-4fb6-bf77-fa8f253f50b4"),
                column: "BadgeDescription",
                value: "Awarded after 11 completed donations.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("cc39fc72-7f5f-4d17-9bfd-9a2f3f5b8db1"),
                column: "BadgeDescription",
                value: "Awarded after 3 completed donations.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("d2cdeef5-2b0e-473d-88b6-9de0237e1a04"),
                column: "BadgeDescription",
                value: "Awarded for donating in a district other than your home district.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e2cdeef5-2b0e-473d-88b6-9de0237e1a05"),
                column: "BadgeDescription",
                value: "Awarded for donating on a Friday or Saturday.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e3f8c38b-3858-45f0-b571-c7ec736dbfee"),
                column: "BadgeDescription",
                value: "Awarded after 10 completed donations.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f2cdeef5-2b0e-473d-88b6-9de0237e1a06"),
                column: "BadgeDescription",
                value: "Awarded for donating during the holy month of Ramadan.");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f6ee9496-e7fb-43c3-8275-ec5f35cf01a1"),
                column: "BadgeDescription",
                value: "Awarded for completing your first donation.");
        }
    }
}
