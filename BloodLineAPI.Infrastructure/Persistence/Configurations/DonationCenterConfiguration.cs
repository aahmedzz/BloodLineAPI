using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations;

public class DonationCenterConfiguration : IEntityTypeConfiguration<DonationCenter>
{
    private static readonly Guid BeniSuefMainBranchId = Guid.Parse("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1");

    public void Configure(EntityTypeBuilder<DonationCenter> builder)
    {
        builder.HasKey(dc => dc.Id);
        builder.Property(dc => dc.Name).IsRequired().HasMaxLength(200);
        builder.Property(dc => dc.Location).HasMaxLength(300);
        builder.Property(dc => dc.AddressDetails).HasMaxLength(500);
        builder.Property(dc => dc.CenterType).HasConversion<string>().HasMaxLength(50);
        builder.Property(dc => dc.Status).HasConversion<string>().HasMaxLength(50);

        builder.HasData(new DonationCenter
        {
            Id = BeniSuefMainBranchId,
            Name = "Beni Suef Main Branch",
            Location = "Beni Suef",
            AddressDetails = "Beni Suef Main Branch",
            Latitude = 29.0420059,
            Longitude = 31.118414,
            CenterType = CenterType.MainBranch,
            Status = CenterStatus.Active,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = null,
            StartTime = new TimeSpan(7, 0, 0),
            EndTime = new TimeSpan(21, 0, 0),
            DescriptionText = "Main branch in Beni Suef.",
            MaxDonorsPerSlot = 10,
            SlotDurationMinutes = 15,
            CreatedAt = new DateTime(2026, 1, 1),
            LastModifiedAt = null,
            CreatedBy = null,
            LastModifiedBy = null
        });
    }
}
