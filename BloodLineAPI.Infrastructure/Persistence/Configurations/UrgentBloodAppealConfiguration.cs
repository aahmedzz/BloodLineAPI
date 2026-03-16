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
    public class UrgentBloodAppealConfiguration : IEntityTypeConfiguration<UrgentBloodAppeal>
    {
        public void Configure(EntityTypeBuilder<UrgentBloodAppeal> builder)
        {
            builder.HasKey(uba => uba.Id);
            builder.Property(uba => uba.Title).IsRequired().HasMaxLength(200);
            builder.Property(uba => uba.Description).HasMaxLength(1000);
            builder.Property(uba => uba.TargetDistrict).HasMaxLength(100);
            builder.HasOne(uba => uba.CreatedByStaff)
                .WithMany(s => s.UrgentBloodAppeals)
                .HasForeignKey(uba => uba.CreatedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(uba => uba.TargetedBloodTypes)
                .WithMany(bt => bt.UrgentBloodAppeals);
        }
    }

}
