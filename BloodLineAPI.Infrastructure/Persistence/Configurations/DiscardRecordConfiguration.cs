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
    public class DiscardRecordConfiguration : IEntityTypeConfiguration<DiscardRecord>
    {
        public void Configure(EntityTypeBuilder<DiscardRecord> builder)
        {
            builder.HasKey(dr => dr.Id);
            builder.Property(dr => dr.ReasonCategory).HasConversion<string>().HasMaxLength(50);
            builder.Property(dr => dr.ReasonDetails).HasMaxLength(500);
            builder.HasOne(dr => dr.BloodBag)
                .WithOne(bb => bb.DiscardRecord)
                .HasForeignKey<DiscardRecord>(dr => dr.BloodBagId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(dr => dr.AuthorizedByStaff)
                .WithMany(s => s.AuthorizedDiscardRecords)
                .HasForeignKey(dr => dr.AuthorizedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
