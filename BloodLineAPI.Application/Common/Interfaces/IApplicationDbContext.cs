using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Donor> Donors { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
