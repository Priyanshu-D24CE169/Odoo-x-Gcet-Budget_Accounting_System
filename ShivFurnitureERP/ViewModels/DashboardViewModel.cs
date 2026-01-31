namespace ShivFurnitureERP.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalOrders { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public IReadOnlyList<string> RevenueLabels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<decimal> RevenueData { get; set; } = Array.Empty<decimal>();
}
