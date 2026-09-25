using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Infrastructure.Persistence.Repositories;
using Identity.Application.Abstractions;
using Identity.Domain.Users;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class IdentityRepository(IApplicationDbContext dbContext)
    : Repository<User, Guid>(dbContext), IIdentityRepository
{
}
