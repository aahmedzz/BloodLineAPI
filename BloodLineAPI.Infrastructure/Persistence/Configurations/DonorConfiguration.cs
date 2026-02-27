using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations;

public class DonorConfiguration : IEntityTypeConfiguration<Donor>
{
    public void Configure(EntityTypeBuilder<Donor> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();
    }
}
