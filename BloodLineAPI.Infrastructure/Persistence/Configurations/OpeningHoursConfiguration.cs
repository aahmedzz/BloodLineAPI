using BloodLineAPI.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations;

public class OpeningHoursConfiguration : IEntityTypeConfiguration<OpeningHours>
{
    private static readonly Guid BeniSuefMainBranchId = Guid.Parse("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1");

    public void Configure(EntityTypeBuilder<OpeningHours> builder)
    {
        builder.HasKey(oh => oh.Id);
        builder.HasIndex(oh => new { oh.CenterId, oh.DayOfWeek }).IsUnique();
        builder.Property(oh => oh.DayOfWeek).HasConversion<int>();

        builder.HasOne(oh => oh.Center)
            .WithMany(c => c.OpeningHours)
            .HasForeignKey(oh => oh.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

        var openingTime = new TimeSpan(7, 0, 0);
        var closingTime = new TimeSpan(21, 0, 0);

        builder.HasData(
            new { Id = Guid.Parse("0b14eefa-72d9-4f83-aad4-6d4e90ca8e10"), CenterId = BeniSuefMainBranchId, DayOfWeek = DayOfWeek.Sunday, IsClosed = false, OpeningTime = openingTime, ClosingTime = closingTime, MaxDonorsPerSlot = (int?)null },
            new { Id = Guid.Parse("7f98f6f7-9556-4fbb-a457-e15457d0656f"), CenterId = BeniSuefMainBranchId, DayOfWeek = DayOfWeek.Monday, IsClosed = false, OpeningTime = openingTime, ClosingTime = closingTime, MaxDonorsPerSlot = (int?)null },
            new { Id = Guid.Parse("9da3ab38-757b-4b96-80a5-3e84f917f4fe"), CenterId = BeniSuefMainBranchId, DayOfWeek = DayOfWeek.Tuesday, IsClosed = false, OpeningTime = openingTime, ClosingTime = closingTime, MaxDonorsPerSlot = (int?)null },
            new { Id = Guid.Parse("0eb9fceb-4f4f-48b5-80c5-63f9da73b763"), CenterId = BeniSuefMainBranchId, DayOfWeek = DayOfWeek.Wednesday, IsClosed = false, OpeningTime = openingTime, ClosingTime = closingTime, MaxDonorsPerSlot = (int?)null },
            new { Id = Guid.Parse("f7ff2d1c-b95a-4f08-806d-9bf8f5f1a036"), CenterId = BeniSuefMainBranchId, DayOfWeek = DayOfWeek.Thursday, IsClosed = false, OpeningTime = openingTime, ClosingTime = closingTime, MaxDonorsPerSlot = (int?)null },
            new { Id = Guid.Parse("2dc3fd35-a567-437f-a53f-c833fddfa0f7"), CenterId = BeniSuefMainBranchId, DayOfWeek = DayOfWeek.Friday, IsClosed = false, OpeningTime = openingTime, ClosingTime = closingTime, MaxDonorsPerSlot = (int?)null },
            new { Id = Guid.Parse("3f9ef858-2962-47d5-95f9-c7e3f2eaeb58"), CenterId = BeniSuefMainBranchId, DayOfWeek = DayOfWeek.Saturday, IsClosed = false, OpeningTime = openingTime, ClosingTime = closingTime, MaxDonorsPerSlot = (int?)null });
    }
}
