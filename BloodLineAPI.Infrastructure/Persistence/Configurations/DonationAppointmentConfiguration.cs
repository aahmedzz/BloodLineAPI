using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
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

            builder.Property(da => da.DonationNumber)
                .UseIdentityColumn();

            builder.Property(da => da.DonationCode)
                .HasComputedColumnSql(
                    "'DTN-' + CAST(YEAR([CreatedAt]) AS VARCHAR(4)) + '-' + " +
                    "CASE WHEN [DonationNumber] < 10000 " +
                    "THEN RIGHT('0000' + CAST([DonationNumber] AS VARCHAR(10)), 4) " +
                    "ELSE CAST([DonationNumber] AS VARCHAR(10)) END",
                    stored: false)
                .HasMaxLength(20);

            builder.HasIndex(da => da.DonationCode).IsUnique();

            builder.Property(da => da.DonationType).HasConversion<string>().HasMaxLength(50);
            builder.Property(da => da.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(da => da.Source).HasConversion<string>().HasMaxLength(50);
            builder.Property(da => da.DonationStatus).HasConversion<string>().HasMaxLength(50);
            builder.Property(da => da.SentToLab).HasDefaultValue(false);
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

            // MedicalScreeningId is stored as a plain column for quick domain-level reference.
            // The actual FK relationship is configured from MedicalScreeningConfiguration
            // (MedicalScreening.DonationAppointmentId -> DonationAppointments).
            builder.Ignore(da => da.MedicalScreening);
        }
    }
}
