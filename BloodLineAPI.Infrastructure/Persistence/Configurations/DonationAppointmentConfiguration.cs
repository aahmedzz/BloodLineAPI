using BloodBankSystem.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodBankSystem.Infrastructure.Persistence.Configurations
{
    public class DonationAppointmentConfiguration : IEntityTypeConfiguration<DonationAppointment>
    {
        public void Configure(EntityTypeBuilder<DonationAppointment> builder)
        {
            builder.HasKey(da => da.Id);
            builder.Property(da => da.DonationType).HasMaxLength(100);
            builder.Property(da => da.Status).HasConversion<string>();
            builder.HasOne(da => da.Donor)
                .WithMany(d => d.DonationAppointments)
                .HasForeignKey(da => da.DonorId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(da => da.DonationCenter)
                .WithMany(dc => dc.DonationAppointments)
                .HasForeignKey(da => da.DonationCenterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
