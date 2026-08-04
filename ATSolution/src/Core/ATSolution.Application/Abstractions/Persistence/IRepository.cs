using ATSolution.Domain.Entities.Common;

namespace ATSolution.Application.Abstractions.Persistence;

public interface IRepository<TEntity, in TId> : IReadRepository<TEntity, TId> where TEntity : class, IEntity<TId>
    where TId : notnull
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    void RemoveRange(IEnumerable<TEntity> entities);
}