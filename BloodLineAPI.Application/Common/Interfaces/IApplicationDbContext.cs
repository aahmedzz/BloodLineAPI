
using BloodBankSystem.Domain.Entities;
using BloodBankSystem.Domain.Entities.BloodEntities;
using BloodBankSystem.Domain.Entities.DonationEntities;
using BloodBankSystem.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IApplicationDbContext
{

    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Staff> Staff { get; }
    DbSet<Donor> Donors { get; }
    DbSet<BloodTypeEntity> BloodTypes { get; }
    DbSet<BloodBag> BloodBags { get; }
    DbSet<BloodComponent> BloodComponents { get; }
    DbSet<BloodTestResult> BloodTestResults { get; }
    DbSet<DonationCenter> DonationCenters { get; }
    DbSet<DonationAppointment> DonationAppointments { get; }
    DbSet<DonationRating> DonationRatings { get; }
    DbSet<MedicalScreening> MedicalScreenings { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<RewardHistory> RewardHistories { get; }
    DbSet<Badge> Badges { get; }
    DbSet<DonorBadge> DonorBadges { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }
    DbSet<DiscardRecord> DiscardRecords { get; }
    DbSet<UrgentBloodAppeal> UrgentBloodAppeals { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
