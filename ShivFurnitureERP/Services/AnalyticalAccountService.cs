using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class AnalyticalAccountService : IAnalyticalAccountService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AnalyticalAccountService> _logger;

    public AnalyticalAccountService(ApplicationDbContext dbContext, ILogger<AnalyticalAccountService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AnalyticalAccount>> GetAccountsAsync(string? search, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = _dbContext.AnalyticalAccounts.AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(a => !a.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.Name.Contains(term));
        }

        return await query
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public Task<AnalyticalAccount?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _dbContext.AnalyticalAccounts.FirstOrDefaultAsync(a => a.AnalyticalAccountId == id, cancellationToken);
    }

    public async Task<AnalyticalAccount> CreateAsync(AnalyticalAccount account, CancellationToken cancellationToken)
    {
        account.CreatedOn = DateTime.UtcNow;
        _dbContext.AnalyticalAccounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task UpdateAsync(AnalyticalAccount updatedAccount, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AnalyticalAccounts.FirstOrDefaultAsync(a => a.AnalyticalAccountId == updatedAccount.AnalyticalAccountId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Analytical account {updatedAccount.AnalyticalAccountId} not found.");
        }

        existing.Name = updatedAccount.Name;
        existing.Description = updatedAccount.Description;
        existing.IsArchived = updatedAccount.IsArchived;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
