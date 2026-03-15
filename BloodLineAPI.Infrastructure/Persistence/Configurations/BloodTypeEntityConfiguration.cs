using BloodBankSystem.Domain.Entities.BloodEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodBankSystem.Infrastructure.Persistence.Configurations
{
    public class BloodTypeEntityConfiguration : IEntityTypeConfiguration<BloodTypeEntity>
    {
        public void Configure(EntityTypeBuilder<BloodTypeEntity> builder)
        {
            builder.HasKey(bt => bt.Id);
            builder.Property(bt => bt.BloodGroupName).HasConversion<string>().IsRequired();
            builder.Property(bt => bt.RhFactor).HasConversion<string>().IsRequired();
            builder.HasIndex(bt => new { bt.BloodGroupName, bt.RhFactor }).IsUnique();
        }
    }
}
