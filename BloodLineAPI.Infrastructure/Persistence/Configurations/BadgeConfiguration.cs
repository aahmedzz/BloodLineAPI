using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
    {
        public void Configure(EntityTypeBuilder<Badge> builder)
        {
            builder.HasKey(b => b.Id);
            builder.HasIndex(b => b.BadgeKey).IsUnique();
            builder.Property(b => b.BadgeKey).IsRequired().HasMaxLength(100);
            builder.Property(b => b.BadgeName).IsRequired().HasMaxLength(100);
            builder.Property(b => b.BadgeNameAr).IsRequired().HasMaxLength(100);
            builder.Property(b => b.BadgeDescription).HasMaxLength(500);
            builder.Property(b => b.BadgeDescriptionAr).HasMaxLength(500);
            builder.Property(b => b.IconUrl).HasMaxLength(500);
            builder.Property(b => b.BadgeType).HasConversion<string>();

            builder.HasData(
                new Badge { Id = Guid.Parse("f6ee9496-e7fb-43c3-8275-ec5f35cf01a1"), BadgeKey = "giver", BadgeName = "Giver", BadgeNameAr = "المعطي", BadgeDescription = "The journey begins! You've earned this title by giving the gift of life and taking your first step toward saving others.", BadgeDescriptionAr = "تبدأ الرحلة! لقد حصلت على هذا اللقب بتقديمك هدية الحياة واتخاذ خطوتك الأولى نحو إنقاذ الآخرين.", IconUrl = "badges/giver.png", BadgeType = BadgeType.Milestone, BonusPoints = 100 },
                new Badge { Id = Guid.Parse("cc39fc72-7f5f-4d17-9bfd-9a2f3f5b8db1"), BadgeKey = "helper", BadgeName = "Helper", BadgeNameAr = "المساعد", BadgeDescription = "A true community pillar! By donating 3 times, you have helped sustain the lives of more than 9 people.", BadgeDescriptionAr = "ركيزة حقيقية للمجتمع! بتبرعك 3 مرات، ساعدت في دعم حياة أكثر من 9 أشخاص.", IconUrl = "badges/helper.png", BadgeType = BadgeType.Milestone, BonusPoints = 300 },
                new Badge { Id = Guid.Parse("89e198c4-d715-4cf6-a85f-7696159f423a"), BadgeKey = "hero", BadgeName = "Hero", BadgeNameAr = "البطل", BadgeDescription = "A title for the brave! Your consistent contributions have made you a real-life hero in the eyes of those you've saved.", BadgeDescriptionAr = "لقب للشجعان! تبرعاتك المستمرة جعلت منك بطلاً حقيقياً في عيون أولئك الذين أنقذتهم.", IconUrl = "badges/hero.png", BadgeType = BadgeType.Milestone, BonusPoints = 500 },
                new Badge { Id = Guid.Parse("e3f8c38b-3858-45f0-b571-c7ec736dbfee"), BadgeKey = "life_saver", BadgeName = "Life Saver", BadgeNameAr = "منقذ الحياة", BadgeDescription = "Your generosity knows no bounds! With 10 donations, you have become a vital protector of countless lives.", BadgeDescriptionAr = "كرمك ليس له حدود! مع 10 تبرعات، أصبحت حامياً حيوياً لحياة لا حصر لها.", IconUrl = "badges/life_saver.png", BadgeType = BadgeType.Milestone, BonusPoints = 800 },
                new Badge { Id = Guid.Parse("c80d19d2-63d9-4fb6-bf77-fa8f253f50b4"), BadgeKey = "monqez", BadgeName = "Monqez", BadgeNameAr = "منقذ", BadgeDescription = "The ultimate elite! You are now a legendary savior in your community, leaving a legacy of hope and saved lives.", BadgeDescriptionAr = "النخبة المطلقة! أنت الآن منقذ أسطوري في مجتمعك، تاركاً إرثاً من الأمل والأرواح التي تم إنقاذها.", IconUrl = "badges/monqez.png", BadgeType = BadgeType.Milestone, BonusPoints = 1000 },
                new Badge { Id = Guid.Parse("a2cdeef5-2b0e-473d-88b6-9de0237e1a01"), BadgeKey = "platelet_guardian", BadgeName = "Platelet Guardian", BadgeNameAr = "حارس الصفائح", BadgeDescription = "Defender of the vulnerable! Your dedicated platelet donations provide strength and hope to those fighting critical conditions.", BadgeDescriptionAr = "مدافع عن الفئات الأكثر ضعفاً! تبرعك بالصفائح الدموية يمنح القوة والأمل للمرضى في الحالات الحرجة.", IconUrl = "badges/platelet_guardian.png", BadgeType = BadgeType.Action, BonusPoints = 200 },
                new Badge { Id = Guid.Parse("b2cdeef5-2b0e-473d-88b6-9de0237e1a02"), BadgeKey = "yellow_gold", BadgeName = "Yellow Gold", BadgeNameAr = "الذهب الأصفر", BadgeDescription = "Liquid gold giver! Your plasma donation helps create life-saving therapies for patients in intensive care.", BadgeDescriptionAr = "معطي الذهب السائل! يساعد تبرعك بالبلازما في توفير العلاجات المنقذة للحياة لمرضى العناية المركزة.", IconUrl = "badges/yellow_gold.png", BadgeType = BadgeType.Action, BonusPoints = 200 },
                new Badge { Id = Guid.Parse("c2cdeef5-2b0e-473d-88b6-9de0237e1a03"), BadgeKey = "triple_giver", BadgeName = "The Triple Giver", BadgeNameAr = "المعطي الثلاثي", BadgeDescription = "The ultimate trifecta! You have shown outstanding dedication by donating whole blood, platelets, and plasma.", BadgeDescriptionAr = "العطاء الثلاثي المتكامل! لقد أظهرت تفانياً استثنائياً بالتبرع بالدم الكامل، والصفائح الدموية، والبلازما.", IconUrl = "badges/triple_giver.png", BadgeType = BadgeType.Action, BonusPoints = 600 },
                new Badge { Id = Guid.Parse("d2cdeef5-2b0e-473d-88b6-9de0237e1a04"), BadgeKey = "traveler_lifesaver", BadgeName = "Traveler Lifesaver", BadgeNameAr = "المنقذ المسافر", BadgeDescription = "A savior without borders! You went the extra mile by donating outside your home district to support another community.", BadgeDescriptionAr = "منقذ بلا حدود! لقد قطعت مسافة إضافية بالتبرع خارج منطقتك السكنية لدعم مجتمع آخر بحاجة للدم.", IconUrl = "badges/traveler_lifesaver.png", BadgeType = BadgeType.Action, BonusPoints = 150 },
                new Badge { Id = Guid.Parse("e2cdeef5-2b0e-473d-88b6-9de0237e1a05"), BadgeKey = "weekend_hero", BadgeName = "Weekend Hero", BadgeNameAr = "بطل عطلة نهاية الأسبوع", BadgeDescription = "Guardian of the weekend! You chose to spend your weekend giving back and ensuring blood is available when it is needed most.", BadgeDescriptionAr = "بطل عطلة نهاية الأسبوع! لقد اخترت قضاء عطلتك في العطاء وتأمين مخزون الدم للحالات الطارئة.", IconUrl = "badges/weekend_hero.png", BadgeType = BadgeType.Action, BonusPoints = 100 },
                new Badge { Id = Guid.Parse("f2cdeef5-2b0e-473d-88b6-9de0237e1a06"), BadgeKey = "ramadan_light", BadgeName = "The Ramadan Light", BadgeNameAr = "نور رمضان", BadgeDescription = "Shedding light in the holy month! Your donation during Ramadan brings blessings and hope to families in need.", BadgeDescriptionAr = "نور يضيء في الشهر الفضيل! تبرعك بالدم خلال شهر رمضان يمنح البركة والأمل للعائلات المحتاجة.", IconUrl = "badges/ramadan_light.png", BadgeType = BadgeType.Action, BonusPoints = 300 },
                new Badge { Id = Guid.Parse("02cdeef5-2b0e-473d-88b6-9de0237e1a07"), BadgeKey = "winter_guard", BadgeName = "Winter Guard", BadgeNameAr = "حارس الشتاء", BadgeDescription = "Warming hearts in the cold! Your winter donation helps keep blood banks warm and ready during seasonal shortages.", BadgeDescriptionAr = "دفء القلوب في برد الشتاء! يساعد تبرعك الشتوي في بقاء بنوك الدم مستعدة خلال فترات نقص المخزون الموسمية.", IconUrl = "badges/winter_guard.png", BadgeType = BadgeType.Action, BonusPoints = 200 },
                new Badge { Id = Guid.Parse("12cdeef5-2b0e-473d-88b6-9de0237e1a08"), BadgeKey = "eid_savior", BadgeName = "Eid Savior", BadgeNameAr = "منقذ العيد", BadgeDescription = "Spreading joy in Eid! Your generous contribution ensures that festive times remain safe for patients undergoing emergency care.", BadgeDescriptionAr = "ناشر الفرحة في العيد! يضمن تبرعك السخي بقاء أوقات الأعياد آمنة للمرضى الذين يحتاجون لرعاية طارئة.", IconUrl = "badges/eid_savior.png", BadgeType = BadgeType.Action, BonusPoints = 250 }
            );
        }
    }
}
