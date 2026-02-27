using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Repositories;
using BloodLineAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Infrastructure.Repositories;

public class GenericRepository<T>(ApplicationDbContext dbContext)
    : IGenericRepository<T> where T : BaseEntity
{
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<T>().AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        dbContext.Set<T>().Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        dbContext.Set<T>().Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
