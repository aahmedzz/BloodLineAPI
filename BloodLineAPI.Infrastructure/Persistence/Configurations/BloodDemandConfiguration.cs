using BloodLineAPI.Domain.Entities.BloodEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class BloodDemandConfiguration : IEntityTypeConfiguration<BloodDemand>
    {
        public void Configure(EntityTypeBuilder<BloodDemand> builder)
        {
            builder.HasKey(bd => bd.Id);

            builder.Property(bd => bd.RequesterName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(bd => bd.Notes)
                .HasMaxLength(1000);

            builder.Property(bd => bd.Priority)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(bd => bd.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(bd => bd.IssuedUnits)
                .HasDefaultValue(0);

            builder.HasOne(bd => bd.BloodType)
                .WithMany(bt => bt.BloodDemands)
                .HasForeignKey(bd => bd.BloodTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
