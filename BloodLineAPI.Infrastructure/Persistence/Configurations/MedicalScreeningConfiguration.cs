using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class MedicalScreeningConfiguration : IEntityTypeConfiguration<MedicalScreening>
    {
        public void Configure(EntityTypeBuilder<MedicalScreening> builder)
        {
            builder.HasKey(ms => ms.Id);
            builder.Property(ms => ms.Weight).HasPrecision(5, 2);
            builder.Property(ms => ms.BloodPressure).HasPrecision(5, 2);
            builder.Property(ms => ms.HemoglobinLevel).HasPrecision(5, 2);
            builder.Property(ms => ms.RejectionReason).HasMaxLength(500);
            builder.HasOne(ms => ms.PerformedByStaff)
                .WithMany(s => s.MedicalScreenings)
                .HasForeignKey(ms => ms.PerformedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
