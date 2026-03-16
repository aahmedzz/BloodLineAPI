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
            builder.Property(d => d.LastName).IsRequired().HasMaxLength(100);
            builder.Property(d => d.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(d => d.NationalId).HasMaxLength(100);
            builder.Property(d => d.Address).HasMaxLength(300);
            builder.Property(d => d.City).HasMaxLength(100);
            builder.Property(d => d.District).HasMaxLength(100);
            builder.HasOne(d => d.BloodType)
                .WithMany(bt => bt.Donors)
                .HasForeignKey("BloodTypeId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            builder.Property(d => d.Gender).HasConversion<string>();
            builder.HasOne(d => d.User)
                .WithOne(u => u.Donor)
                .HasForeignKey<Donor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
