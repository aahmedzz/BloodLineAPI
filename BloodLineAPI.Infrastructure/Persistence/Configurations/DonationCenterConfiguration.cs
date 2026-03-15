using BloodBankSystem.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodBankSystem.Infrastructure.Persistence.Configurations
{
    public class DonationCenterConfiguration : IEntityTypeConfiguration<DonationCenter>
    {
        public void Configure(EntityTypeBuilder<DonationCenter> builder)
        {
            builder.HasKey(dc => dc.Id);
            builder.Property(dc => dc.Name).IsRequired().HasMaxLength(200);
            builder.Property(dc => dc.Location).HasMaxLength(300);
            builder.Property(dc => dc.AddressDetails).HasMaxLength(500);
            builder.Property(dc => dc.CenterType).HasMaxLength(50);
        }
    }
}
