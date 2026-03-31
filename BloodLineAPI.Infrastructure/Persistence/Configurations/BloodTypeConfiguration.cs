using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class BloodTypeConfiguration : IEntityTypeConfiguration<BloodType>
    {
        public void Configure(EntityTypeBuilder<BloodType> builder)
        {
            builder.HasKey(bt => bt.Id);
            builder.Property(bt => bt.BloodGroupName).HasConversion<string>().IsRequired();
            builder.Property(bt => bt.RhFactor).HasConversion<string>().IsRequired();
            builder.HasIndex(bt => new { bt.BloodGroupName, bt.RhFactor }).IsUnique();

            builder.HasData(
                new BloodType { Id = 1, BloodGroupName = BloodGroupName.A, RhFactor = RhFactor.Positive },
                new BloodType { Id = 2, BloodGroupName = BloodGroupName.A, RhFactor = RhFactor.Negative },
                new BloodType { Id = 3, BloodGroupName = BloodGroupName.B, RhFactor = RhFactor.Positive },
                new BloodType { Id = 4, BloodGroupName = BloodGroupName.B, RhFactor = RhFactor.Negative },
                new BloodType { Id = 5, BloodGroupName = BloodGroupName.AB, RhFactor = RhFactor.Positive },
                new BloodType { Id = 6, BloodGroupName = BloodGroupName.AB, RhFactor = RhFactor.Negative },
                new BloodType { Id = 7, BloodGroupName = BloodGroupName.O, RhFactor = RhFactor.Positive },
                new BloodType { Id = 8, BloodGroupName = BloodGroupName.O, RhFactor = RhFactor.Negative }
            );
        }
    }
}
