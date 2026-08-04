using System.ComponentModel.DataAnnotations;

namespace ATSolution.Domain.Entities.Common;

public abstract class Entity<TId> : IEntity<TId>
    where TId : notnull
{
    [Key]
    public TId Id { get; protected set; } = default!;
}