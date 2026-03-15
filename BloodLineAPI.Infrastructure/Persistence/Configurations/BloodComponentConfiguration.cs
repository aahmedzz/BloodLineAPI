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
    public class BloodComponentConfiguration : IEntityTypeConfiguration<BloodComponent>
    {
        public void Configure(EntityTypeBuilder<BloodComponent> builder)
        {
            builder.HasKey(bc => bc.Id);
            builder.Property(bc => bc.ComponentType).IsRequired().HasMaxLength(100);
            builder.Property(bc => bc.Volume).HasPrecision(8, 2);
            builder.HasOne(bc => bc.BloodBag)
                .WithMany(bb => bb.BloodComponents)
                .HasForeignKey(bc => bc.BloodBagId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
