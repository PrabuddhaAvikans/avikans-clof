using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ATSolution.Infrastructure.Persistence.Repositories;

public class EntityRepository<TEntity> : IEntityRepository<TEntity>
    where TEntity : class
{
    protected DbSet<TEntity> Set { get; }

    public EntityRepository(SqlDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        Set = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query(bool asNoTracking = false)
    {
        return asNoTracking ? Set.AsNoTracking() : Set;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await Set.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Set.RemoveRange(entities);
    }
}
