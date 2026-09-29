using ATSolution.Domain.Entities.Common;
using Inventory.Domain.Common;

namespace Inventory.Domain.Units;

public class UnitOfMeasure : Entity<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Status { get; private set; } = EntityStatuses.Active;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static UnitOfMeasure Create(string code, string name, string status = EntityStatuses.Active)
    {
        var now = DateTimeOffset.UtcNow;
        return new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Status = status,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(string code, string name, string status)
    {
        Code = code.Trim();
        Name = name.Trim();
        Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
