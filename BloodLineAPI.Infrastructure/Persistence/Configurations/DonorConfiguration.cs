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

            builder.Property(d => d.DonorNumber)
                .UseIdentityColumn();

            builder.Property(d => d.DonorCode)
                .HasComputedColumnSql(
                    "'DNR-' + CAST(YEAR([CreatedAt]) AS VARCHAR(4)) + '-' + " +
                    "CASE WHEN [DonorNumber] < 10000 " +
                    "THEN RIGHT('0000' + CAST([DonorNumber] AS VARCHAR(10)), 4) " +
                    "ELSE CAST([DonorNumber] AS VARCHAR(10)) END",
                    stored: false)
                .HasMaxLength(20);

            builder.HasIndex(d => d.DonorCode).IsUnique();

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
