using ShivFurnitureERP.Repositories;
using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IDashboardRepository dashboardRepository, ILogger<DashboardService> logger)
    {
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building dashboard view model");

        var totalCustomers = await _dashboardRepository.GetCustomerCountAsync(cancellationToken);
        var totalOrders = await _dashboardRepository.GetOrderCountAsync(cancellationToken);
        var currentMonthRevenue = await _dashboardRepository.GetCurrentMonthRevenueAsync(cancellationToken);
        var revenueSeries = await _dashboardRepository.GetRecentRevenueAsync(6, cancellationToken);

        return new DashboardViewModel
        {
            TotalCustomers = totalCustomers,
            TotalOrders = totalOrders,
            CurrentMonthRevenue = currentMonthRevenue,
            RevenueLabels = revenueSeries.Select(point => point.Label).ToList(),
            RevenueData = revenueSeries.Select(point => point.Amount).ToList()
        };
    }
}
