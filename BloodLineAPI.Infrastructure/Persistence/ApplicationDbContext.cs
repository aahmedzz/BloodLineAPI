using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Entities.Users;
using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; using Microsoft.AspNetCore.Identity;

namespace BloodLineAPI.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options), IApplicationDbContext
{
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<Donor> Donors { get; set; } = null!;
    public DbSet<BloodType> BloodTypes { get; set; } = null!;
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
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = utcNow;
                }

                entry.Entity.LastModifiedAt = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = utcNow;
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
