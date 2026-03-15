using BloodBankSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodBankSystem.Infrastructure.Persistence.Configurations
{
    public class RewardHistoryConfiguration : IEntityTypeConfiguration<RewardHistory>
    {
        public void Configure(EntityTypeBuilder<RewardHistory> builder)
        {
            builder.HasKey(rh => rh.Id);
            builder.Property(rh => rh.ActionType).IsRequired().HasMaxLength(100);
            builder.HasOne(rh => rh.Donor)
                .WithMany(d => d.RewardHistories)
                .HasForeignKey(rh => rh.DonorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
