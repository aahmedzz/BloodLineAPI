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
    public class BloodTestResultConfiguration : IEntityTypeConfiguration<BloodTestResult>
    {
        public void Configure(EntityTypeBuilder<BloodTestResult> builder)
        {
            builder.HasKey(bt => bt.Id);
            builder.Property(bt => bt.TestFileUrl).HasMaxLength(500);
            builder.Property(bt => bt.HepatitisCResult).HasMaxLength(50);
            builder.Property(bt => bt.HepatitisBResult).HasMaxLength(50);
            builder.Property(bt => bt.HivResult).HasMaxLength(50);
            builder.Property(bt => bt.SyphilisResult).HasMaxLength(50);
            builder.Property(bt => bt.Notes).HasMaxLength(1000);
            builder.HasOne(bt => bt.ConfirmedBloodType)
                .WithMany()
                .HasForeignKey(bt => bt.ConfirmedBloodTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            builder.HasOne(bt => bt.BloodBag)
                .WithMany(bb => bb.BloodTestResults)
                .HasForeignKey(bt => bt.BloodBagId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(bt => bt.TestedByStaff)
                .WithMany(s => s.BloodTestResults)
                .HasForeignKey(bt => bt.TestedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
