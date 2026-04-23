using BloodLineAPI.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations;

public class HealthPreScreeningConfiguration : IEntityTypeConfiguration<HealthPreScreening>
{
    public void Configure(EntityTypeBuilder<HealthPreScreening> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Donor)
            .WithMany(d => d.HealthPreScreenings)
            .HasForeignKey(h => h.DonorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
