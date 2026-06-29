using BloodLineAPI.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class WeeklyBloodTypeTargetConfiguration : IEntityTypeConfiguration<BloodTypeTargets>
    {
        private static readonly Guid BeniSuefMainBranchId = Guid.Parse("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1");

        public void Configure(EntityTypeBuilder<BloodTypeTargets> builder)
        {
            builder.ToTable("BloodTypeTargets");
            builder.HasKey(w => w.Id);

            builder.Property(w => w.BloodType)
                .IsRequired()
                .HasMaxLength(10);

            builder.HasIndex(w => new { w.DonationCenterId, w.BloodType })
                .IsUnique();

            builder.HasOne(w => w.DonationCenter)
                .WithMany(c => c.BloodTypeTargets)
                .HasForeignKey(w => w.DonationCenterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasData(
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f001-4a92-8b0a-2fc73f6c0001"), DonationCenterId = BeniSuefMainBranchId, BloodType = "A+", TargetCount = 40 },
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f002-4a92-8b0a-2fc73f6c0002"), DonationCenterId = BeniSuefMainBranchId, BloodType = "A-", TargetCount = 10 },
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f003-4a92-8b0a-2fc73f6c0003"), DonationCenterId = BeniSuefMainBranchId, BloodType = "B+", TargetCount = 50 },
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f004-4a92-8b0a-2fc73f6c0004"), DonationCenterId = BeniSuefMainBranchId, BloodType = "B-", TargetCount = 10 },
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f005-4a92-8b0a-2fc73f6c0005"), DonationCenterId = BeniSuefMainBranchId, BloodType = "AB+", TargetCount = 20 },
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f006-4a92-8b0a-2fc73f6c0006"), DonationCenterId = BeniSuefMainBranchId, BloodType = "AB-", TargetCount = 5 },
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f007-4a92-8b0a-2fc73f6c0007"), DonationCenterId = BeniSuefMainBranchId, BloodType = "O+", TargetCount = 60 },
                new BloodTypeTargets { Id = Guid.Parse("b5b4d5b7-f008-4a92-8b0a-2fc73f6c0008"), DonationCenterId = BeniSuefMainBranchId, BloodType = "O-", TargetCount = 15 }
            );
        }
    }
}
