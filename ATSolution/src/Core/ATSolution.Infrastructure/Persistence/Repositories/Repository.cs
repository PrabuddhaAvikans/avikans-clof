using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ATSolution.Infrastructure.Persistence.Repositories;

public abstract class Repository<TEntity, TId>(IApplicationDbContext dbContext) : IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    protected DbSet<TEntity> _dbSet { get; } = dbContext.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TId id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();

        return await query.FirstOrDefaultAsync(
            entity => entity.Id.Equals(id),
            cancellationToken);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();

        return await query.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            return await _dbSet.CountAsync(cancellationToken);
        }

        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _dbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _dbSet.RemoveRange(entities);
    }
}
