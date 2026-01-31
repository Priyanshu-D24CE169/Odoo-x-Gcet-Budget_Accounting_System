using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
