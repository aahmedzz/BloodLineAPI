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
    public class StaffConfiguration : IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.EmployeeIdentifier).IsRequired().HasMaxLength(50);
            builder.HasIndex(s => s.EmployeeIdentifier).IsUnique();
            builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.SecondName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.ThirdName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.FourthName).IsRequired(false).HasMaxLength(100);
            builder.Property(s => s.PhoneNumber).HasMaxLength(20);
            builder.Property(s => s.Address).HasMaxLength(300);
            builder.Property(s => s.DepartmentName).HasMaxLength(100);
            builder.Ignore(s => s.FullName);
            builder.HasOne(s => s.User)
                .WithOne(u => u.Staff)
                .HasForeignKey<Staff>(s => s.Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
