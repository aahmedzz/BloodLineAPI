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
    public class DonorBadgeConfiguration : IEntityTypeConfiguration<DonorBadge>
    {
        public void Configure(EntityTypeBuilder<DonorBadge> builder)
        {
            builder.HasKey(db => db.Id);
            builder.HasOne(db => db.Donor)
                .WithMany(d => d.DonorBadges)
                .HasForeignKey(db => db.DonorId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(db => db.Badge)
                .WithMany(b => b.DonorBadges)
                .HasForeignKey(db => db.BadgeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
