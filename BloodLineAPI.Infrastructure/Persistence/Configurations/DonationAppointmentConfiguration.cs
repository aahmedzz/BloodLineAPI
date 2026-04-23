using BloodLineAPI.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class DonationAppointmentConfiguration : IEntityTypeConfiguration<DonationAppointment>
    {
        public void Configure(EntityTypeBuilder<DonationAppointment> builder)
        {
            builder.HasKey(da => da.Id);
            builder.Property(da => da.DonationType).HasConversion<string>().HasMaxLength(50);
            builder.Property(da => da.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(da => da.CancellationReason).HasMaxLength(500);
            builder.Property(da => da.RowVersion).IsRowVersion();
            builder.HasOne(da => da.Donor)
                .WithMany(d => d.DonationAppointments)
                .HasForeignKey(da => da.DonorId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(da => da.DonationCenter)
                .WithMany(dc => dc.DonationAppointments)
                .HasForeignKey(da => da.DonationCenterId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(da => da.HealthPreScreening)
                .WithMany()
                .HasForeignKey(da => da.HealthPreScreeningId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
