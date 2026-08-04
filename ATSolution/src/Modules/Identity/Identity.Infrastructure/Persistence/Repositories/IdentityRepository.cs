using ATSolution.Application.Abstractions.Persistence;
using Identity.Application.Abstractions;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class IdentityRepository : IIdentityRepository
{
    private readonly IApplicationDbContext _dbContext;

    public IdentityRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<User>()
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public Task<User?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<User>()
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public Task<User?> GetByEmailForUpdateAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<User>()
            .FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _dbContext
            .Set<User>()
            .AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _dbContext.Set<User>().Update(user);
    }

    public void Remove(User user)
    {
        _dbContext.Set<User>().Remove(user);
    }
}