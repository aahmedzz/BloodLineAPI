using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class IssuanceRecordConfiguration : IEntityTypeConfiguration<IssuanceRecord>
    {
        public void Configure(EntityTypeBuilder<IssuanceRecord> builder)
        {
            builder.HasKey(ir => ir.Id);
            builder.Property(ir => ir.RecipientName).IsRequired().HasMaxLength(200);
            builder.Property(ir => ir.NationalId).IsRequired().HasMaxLength(14);
            builder.Property(ir => ir.Phone).HasMaxLength(20);
            builder.Property(ir => ir.Reason).IsRequired().HasMaxLength(500);
            builder.HasOne(ir => ir.BloodBag)
                .WithOne(bb => bb.IssuanceRecord)
                .HasForeignKey<IssuanceRecord>(ir => ir.BloodBagId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(ir => ir.IssuedByStaff)
                .WithMany(s => s.IssuedBloodBags)
                .HasForeignKey(ir => ir.IssuedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ir => ir.BloodDemand)
                .WithMany(bd => bd.IssuanceRecords)
                .HasForeignKey(ir => ir.BloodDemandId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
