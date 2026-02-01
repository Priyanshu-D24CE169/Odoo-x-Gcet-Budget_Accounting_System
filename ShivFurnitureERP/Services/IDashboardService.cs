using System;
using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(
        DateTime? start = null,
        DateTime? end = null,
        int? analyticalAccountId = null,
        CancellationToken cancellationToken = default);
}
