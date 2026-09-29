namespace ATSolution.Application.Abstractions.Persistence;

public interface IEntityRepository<TEntity> : IWriteRepository<TEntity>
    where TEntity : class
{
    IQueryable<TEntity> Query(bool asNoTracking = false);
}
