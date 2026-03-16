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
    public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
    {
        public void Configure(EntityTypeBuilder<Badge> builder)
        {
            builder.HasKey(b => b.Id);
            builder.Property(b => b.BadgeName).IsRequired().HasMaxLength(100);
            builder.Property(b => b.BadgeDescription).HasMaxLength(500);
            builder.Property(b => b.IconUrl).HasMaxLength(500);
        }
    }
}
