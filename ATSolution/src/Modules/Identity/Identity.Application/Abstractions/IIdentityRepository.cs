using ATSolution.Application.Abstractions.Persistence;
using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IIdentityRepository : IRepository<User, Guid>
{
}
