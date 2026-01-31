using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IAnalyticalAccountService
{
    Task<IReadOnlyList<AnalyticalAccount>> GetAccountsAsync(string? search, bool includeArchived, CancellationToken cancellationToken);
    Task<AnalyticalAccount?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<AnalyticalAccount> CreateAsync(AnalyticalAccount account, CancellationToken cancellationToken);
    Task UpdateAsync(AnalyticalAccount account, CancellationToken cancellationToken);
}
