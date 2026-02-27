using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Donor> Donors => Set<Donor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
