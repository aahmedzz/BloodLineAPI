using BloodBankSystem.Domain.Entities;
using BloodBankSystem.Domain.Entities.BloodEntities;
using BloodBankSystem.Domain.Entities.DonationEntities;
using BloodBankSystem.Domain.Entities.Users;
using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<Donor> Donors { get; set; } = null!;
    public DbSet<BloodTypeEntity> BloodTypes { get; set; } = null!;
    public DbSet<BloodBag> BloodBags { get; set; } = null!;
    public DbSet<BloodComponent> BloodComponents { get; set; } = null!;
    public DbSet<BloodTestResult> BloodTestResults { get; set; } = null!;
    public DbSet<DonationCenter> DonationCenters { get; set; } = null!;
    public DbSet<DonationAppointment> DonationAppointments { get; set; } = null!;
    public DbSet<DonationRating> DonationRatings { get; set; } = null!;
    public DbSet<MedicalScreening> MedicalScreenings { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<RewardHistory> RewardHistories { get; set; } = null!;
    public DbSet<Badge> Badges { get; set; } = null!;
    public DbSet<DonorBadge> DonorBadges { get; set; } = null!;
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
    public DbSet<DiscardRecord> DiscardRecords { get; set; } = null!;
    public DbSet<UrgentBloodAppeal> UrgentBloodAppeals { get; set; } = null!;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
