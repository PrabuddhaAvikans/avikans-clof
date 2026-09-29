using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Domain.Entities.Common;
using ATSolution.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ATSolution.Infrastructure.Persistence.Repositories;

public class Repository<TEntity, TId> : EntityRepository<TEntity>, IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    public Repository(SqlDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = asNoTracking ? Set.AsNoTracking() : Set.AsQueryable();

        return await query.FirstOrDefaultAsync(
            entity => entity.Id.Equals(id),
            cancellationToken);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = asNoTracking ? Set.AsNoTracking() : Set.AsQueryable();

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = asNoTracking ? Set.AsNoTracking() : Set.AsQueryable();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = asNoTracking ? Set.AsNoTracking() : Set.AsQueryable();

        return await query.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await Set.AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            return await Set.CountAsync(cancellationToken);
        }

        return await Set.CountAsync(predicate, cancellationToken);
    }
}
