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
    public class DonorConfiguration : IEntityTypeConfiguration<Donor>
    {
        public void Configure(EntityTypeBuilder<Donor> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(d => d.SecondName).IsRequired().HasMaxLength(100);
            builder.Property(d => d.ThirdName).IsRequired().HasMaxLength(100);
            builder.Property(d => d.FourthName).IsRequired(false).HasMaxLength(100);
            builder.Property(d => d.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(d => d.NationalId).HasMaxLength(100);
            builder.Property(d => d.Address).IsRequired(false).HasMaxLength(300);
            builder.Property(d => d.Governorate).IsRequired(false).HasMaxLength(100);
            builder.Property(d => d.District).IsRequired(false).HasMaxLength(100);
            builder.Property(d => d.Area).IsRequired(false).HasMaxLength(100);
            builder.Property(d => d.WeightKg).HasPrecision(5, 2);
            builder.Property(d => d.IsRegistrationCompleted).HasDefaultValue(false);
            builder.Property(d => d.MonthlyPoints).HasDefaultValue(0);
            builder.Property(d => d.TotalDonationCount).HasDefaultValue(0);
            builder.Property(d => d.TotalPoints).HasDefaultValue(0);
            builder.HasOne(d => d.BloodType)
                .WithMany(bt => bt.Donors)
                .HasForeignKey(d => d.BloodTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            builder.Property(d => d.Gender).HasConversion<string>();
            builder.HasOne(d => d.User)
                .WithOne(u => u.Donor)
                .HasForeignKey<Donor>(d => d.Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
