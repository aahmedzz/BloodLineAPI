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
        }
    }
}
