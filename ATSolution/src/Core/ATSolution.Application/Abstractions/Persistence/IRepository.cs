using ATSolution.Domain.Entities.Common;

namespace ATSolution.Application.Abstractions.Persistence;

public interface IRepository<TEntity, in TId> : IReadRepository<TEntity, TId>, IWriteRepository<TEntity>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
}
