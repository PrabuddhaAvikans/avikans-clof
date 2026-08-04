using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ATSolution.Infrastructure.Persistence;


public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly SqlDbContext _context;
    private bool _disposed = false;

    public UnitOfWork(SqlDbContext context)
    {
        _context = context;
    }
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var executionStrategy =
            _context.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(
            async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        cancellationToken);

                try
                {
                    var result = await operation(cancellationToken);

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    throw;
                }
            });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        this._disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}