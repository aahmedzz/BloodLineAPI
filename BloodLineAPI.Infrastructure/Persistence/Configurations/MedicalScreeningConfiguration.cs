using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations;

public class MedicalScreeningConfiguration : IEntityTypeConfiguration<MedicalScreening>
{
    public void Configure(EntityTypeBuilder<MedicalScreening> builder)
    {
        builder.HasKey(ms => ms.Id);
        builder.Property(ms => ms.Weight).HasPrecision(5, 2);
        builder.Property(ms => ms.SystolicBP).HasPrecision(5, 2);
        builder.Property(ms => ms.DiastolicBP).HasPrecision(5, 2);
        builder.Property(ms => ms.HemoglobinLevel).HasPrecision(5, 2);
        builder.Property(ms => ms.Temperature).HasPrecision(4, 1);
        builder.Property(ms => ms.PulseRate).HasPrecision(5, 1);
        builder.Property(ms => ms.ChronicDiseaseDetails).HasMaxLength(1000);
        builder.Property(ms => ms.RejectionReason).HasMaxLength(500);

        builder.HasOne(ms => ms.Donor)
            .WithMany(d => d.MedicalScreenings)
            .HasForeignKey(ms => ms.DonorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ms => ms.PerformedByStaff)
            .WithMany(s => s.MedicalScreenings)
            .HasForeignKey(ms => ms.PerformedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ms => ms.DonationAppointment)
            .WithMany()
            .HasForeignKey(ms => ms.DonationAppointmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
