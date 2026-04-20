using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
    {
        public void Configure(EntityTypeBuilder<PointTransaction> builder)
        {
            builder.HasKey(pt => pt.Id);

            builder.Property(pt => pt.ActionType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(pt => pt.Description)
                .HasMaxLength(500);

            builder.Property(pt => pt.MonthKey)
                .IsRequired()
                .HasMaxLength(7);

            builder.HasIndex(pt => new { pt.DonorId, pt.MonthKey });
            builder.HasIndex(pt => new { pt.DonorId, pt.ActionType });
            builder.HasIndex(pt => new { pt.MonthKey, pt.Points });

            builder.HasOne(pt => pt.Donor)
                .WithMany(d => d.PointTransactions)
                .HasForeignKey(pt => pt.DonorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
