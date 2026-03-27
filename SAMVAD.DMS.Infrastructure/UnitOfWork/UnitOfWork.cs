using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAMVAD.DMS.Domain.Interfaces;
using SAMVAD.DMS.Infrastructure.Data;

namespace SAMVAD.DMS.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<T> Query<T>() where T : class
    {
        return _context.Set<T>().AsQueryable();
    }

    public async Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        await _context.Set<T>().AddAsync(entity, cancellationToken);
    }

    public void Update<T>(T entity) where T : class
    {
        _context.Set<T>().Update(entity);
    }

    public void Remove<T>(T entity) where T : class
    {
        _context.Set<T>().Remove(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency issue while saving changes.");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database issue while saving changes.");
            throw;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}