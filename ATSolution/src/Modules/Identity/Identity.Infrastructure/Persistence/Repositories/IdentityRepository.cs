using ATSolution.Infrastructure.Persistence.Data;
using ATSolution.Infrastructure.Persistence.Repositories;
using Identity.Application.Abstractions;
using Identity.Domain.Users;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class IdentityRepository(SqlDbContext dbContext)
    : Repository<User, Guid>(dbContext), IIdentityRepository
{
}
