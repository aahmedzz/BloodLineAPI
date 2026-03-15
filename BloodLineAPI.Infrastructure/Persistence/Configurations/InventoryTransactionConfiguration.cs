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
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.HasKey(it => it.Id);
            builder.Property(it => it.PreviousStatus).HasMaxLength(50);
            builder.Property(it => it.NewStatus).HasMaxLength(50);
            builder.HasOne(it => it.BloodBag)
                .WithMany(bb => bb.InventoryTransactions)
                .HasForeignKey(it => it.BloodBagId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(it => it.ExecutedByStaff)
                .WithMany(s => s.InventoryTransactions)
                .HasForeignKey(it => it.ExecutedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
                  }
    }
}
