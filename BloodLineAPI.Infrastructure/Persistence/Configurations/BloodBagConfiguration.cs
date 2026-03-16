using BloodLineAPI.Domain.Entities.BloodEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class BloodBagConfiguration : IEntityTypeConfiguration<BloodBag>
    {
        public void Configure(EntityTypeBuilder<BloodBag> builder)
        {
            builder.HasKey(bb => bb.Id);
            builder.Property(bb => bb.SerialNumber).IsRequired().HasMaxLength(100);
            builder.HasIndex(bb => bb.SerialNumber).IsUnique();
            builder.Property(bb => bb.Volume).HasPrecision(8, 2);
            builder.Property(bb => bb.Status).HasConversion<string>();
            builder.HasOne(bb => bb.BloodType)
                .WithMany(bt => bt.BloodBags)
                .HasForeignKey(bb => bb.BloodTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(bb => bb.CollectedByStaff)
                .WithMany(s => s.CollectedBloodBags)
                .HasForeignKey(bb => bb.CollectedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(bb => bb.DonationAppointment)
                .WithOne(da => da.BloodBag)
                .HasForeignKey<BloodBag>(bb => bb.DonationAppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
