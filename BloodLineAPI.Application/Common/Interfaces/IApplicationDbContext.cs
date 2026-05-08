
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Staff> Staff { get; }
    DbSet<Donor> Donors { get; }
    DbSet<BloodType> BloodTypes { get; }
    DbSet<BloodBag> BloodBags { get; }
    DbSet<BloodComponent> BloodComponents { get; }
    DbSet<BloodTestResult> BloodTestResults { get; }
    DbSet<DonationCenter> DonationCenters { get; }
    DbSet<DonationAppointment> DonationAppointments { get; }
    DbSet<HealthPreScreening> HealthPreScreenings { get; }
    DbSet<OpeningHours> OpeningHours { get; }
    DbSet<CenterExclusion> CenterExclusions { get; }
    DbSet<DonationRating> DonationRatings { get; }
    DbSet<MedicalScreening> MedicalScreenings { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<PointTransaction> PointTransactions { get; }
    DbSet<Badge> Badges { get; }
    DbSet<DonorBadge> DonorBadges { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }
    DbSet<DiscardRecord> DiscardRecords { get; }
    DbSet<UrgentBloodAppeal> UrgentBloodAppeals { get; }
    DbSet<DeviceToken> DeviceTokens { get; }
    DbSet<BloodStockThreshold> BloodStockThresholds { get; }
    DbSet<ChatConversation> ChatConversations { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
