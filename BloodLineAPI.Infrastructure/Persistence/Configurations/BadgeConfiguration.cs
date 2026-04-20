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
            builder.Property(b => b.IconUrl).HasMaxLength(500);
            builder.Property(b => b.BadgeType).HasConversion<string>();

            builder.HasData(
                new Badge { Id = Guid.Parse("f6ee9496-e7fb-43c3-8275-ec5f35cf01a1"), BadgeKey = "giver", BadgeName = "Giver", BadgeNameAr = "المعطي", BadgeDescription = "Awarded for completing your first donation.", IconUrl = "badges/giver.png", BadgeType = BadgeType.Milestone, BonusPoints = 50 },
                new Badge { Id = Guid.Parse("cc39fc72-7f5f-4d17-9bfd-9a2f3f5b8db1"), BadgeKey = "helper", BadgeName = "Helper", BadgeNameAr = "المساعد", BadgeDescription = "Awarded after 3 completed donations.", IconUrl = "badges/helper.png", BadgeType = BadgeType.Milestone, BonusPoints = 75 },
                new Badge { Id = Guid.Parse("89e198c4-d715-4cf6-a85f-7696159f423a"), BadgeKey = "hero", BadgeName = "Hero", BadgeNameAr = "البطل", BadgeDescription = "Awarded after 5 completed donations.", IconUrl = "badges/hero.png", BadgeType = BadgeType.Milestone, BonusPoints = 100 },
                new Badge { Id = Guid.Parse("e3f8c38b-3858-45f0-b571-c7ec736dbfee"), BadgeKey = "life_saver", BadgeName = "Life Saver", BadgeNameAr = "منقذ الحياة", BadgeDescription = "Awarded after 10 completed donations.", IconUrl = "badges/life_saver.png", BadgeType = BadgeType.Milestone, BonusPoints = 150 },
                new Badge { Id = Guid.Parse("c80d19d2-63d9-4fb6-bf77-fa8f253f50b4"), BadgeKey = "monqez", BadgeName = "Monqez", BadgeNameAr = "منقذ", BadgeDescription = "Awarded after 20 completed donations.", IconUrl = "badges/monqez.png", BadgeType = BadgeType.Milestone, BonusPoints = 300 },
                new Badge { Id = Guid.Parse("2ef1880e-ca8b-4383-8584-3b31f1fb9448"), BadgeKey = "responder", BadgeName = "Responder", BadgeNameAr = "المستجيب", BadgeDescription = "Awarded for the first emergency donation.", IconUrl = "badges/responder.png", BadgeType = BadgeType.Action, BonusPoints = 100 },
                new Badge { Id = Guid.Parse("db1d6774-bf5f-4fdd-aea3-b3865e6a546d"), BadgeKey = "golden_blood", BadgeName = "Golden Blood", BadgeNameAr = "الدم الذهبي", BadgeDescription = "Awarded for donors with rare blood type O- or AB-.", IconUrl = "badges/golden_blood.png", BadgeType = BadgeType.Action, BonusPoints = 100 },
                new Badge { Id = Guid.Parse("7db611c5-b017-48d3-84fb-2273d61938df"), BadgeKey = "ambassador", BadgeName = "Ambassador", BadgeNameAr = "السفير", BadgeDescription = "Awarded after sharing 20 urgent requests.", IconUrl = "badges/ambassador.png", BadgeType = BadgeType.Action, BonusPoints = 80 },
                new Badge { Id = Guid.Parse("e98fa758-1395-4ca1-a17c-51b965ef8f77"), BadgeKey = "night_owl", BadgeName = "Night Owl", BadgeNameAr = "بومة الليل", BadgeDescription = "Awarded for donating after midnight.", IconUrl = "badges/night_owl.png", BadgeType = BadgeType.Action, BonusPoints = 60 }
            );
        }
    }
}
