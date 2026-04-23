using BloodLineAPI.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations;

public class CenterExclusionConfiguration : IEntityTypeConfiguration<CenterExclusion>
{
    public void Configure(EntityTypeBuilder<CenterExclusion> builder)
    {
        builder.HasKey(ce => ce.Id);
        builder.HasIndex(ce => new { ce.CenterId, ce.Date }).IsUnique();
        builder.Property(ce => ce.Reason).HasMaxLength(300);

        builder.HasOne(ce => ce.Center)
            .WithMany(c => c.CenterExclusions)
            .HasForeignKey(ce => ce.CenterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
