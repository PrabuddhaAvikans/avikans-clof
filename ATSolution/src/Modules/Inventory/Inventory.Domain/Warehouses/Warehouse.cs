using ATSolution.Domain.Entities.Common;
using Inventory.Domain.Common;

namespace Inventory.Domain.Warehouses;

public class Warehouse : Entity<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }
    public string Status { get; private set; } = EntityStatuses.Active;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Warehouse Create(string code, string name, string? address = null, string status = EntityStatuses.Active)
    {
        var now = DateTimeOffset.UtcNow;
        return new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Address = address,
            Status = status,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(string code, string name, string? address, string status)
    {
        Code = code.Trim();
        Name = name.Trim();
        Address = address;
        Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
