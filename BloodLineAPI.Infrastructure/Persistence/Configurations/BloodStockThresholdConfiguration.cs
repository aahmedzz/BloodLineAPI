using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class BloodStockThresholdConfiguration : IEntityTypeConfiguration<BloodStockThreshold>
    {
        public void Configure(EntityTypeBuilder<BloodStockThreshold> builder)
        {
            builder.HasKey(t => t.Id);
            builder.HasOne(t => t.BloodType)
                .WithMany()
                .HasForeignKey(t => t.BloodTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasData(
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000001"), BloodTypeId = 1, LowThreshold = 10, CriticalThreshold = 5 },
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000002"), BloodTypeId = 2, LowThreshold = 8, CriticalThreshold = 4 },
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000003"), BloodTypeId = 3, LowThreshold = 12, CriticalThreshold = 6 },
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000004"), BloodTypeId = 4, LowThreshold = 10, CriticalThreshold = 5 },
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000005"), BloodTypeId = 5, LowThreshold = 5, CriticalThreshold = 2 },
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000006"), BloodTypeId = 6, LowThreshold = 5, CriticalThreshold = 2 },
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000007"), BloodTypeId = 7, LowThreshold = 15, CriticalThreshold = 7 },
                new BloodStockThreshold { Id = Guid.Parse("f0c1b2a3-9876-4321-b123-abcdef000008"), BloodTypeId = 8, LowThreshold = 10, CriticalThreshold = 5 }
            );
        }
    }
}
