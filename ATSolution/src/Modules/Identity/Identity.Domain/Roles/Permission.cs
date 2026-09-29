using ATSolution.Domain.Entities.Common;

namespace Identity.Domain.Roles;

public class Permission : Entity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Module { get; private set; } = null!;
    public string Action { get; private set; } = null!;

    public static Permission Create(string module, string action)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Module = module,
            Action = action,
            Code = $"{module}:{action}",
        };
    }
}
