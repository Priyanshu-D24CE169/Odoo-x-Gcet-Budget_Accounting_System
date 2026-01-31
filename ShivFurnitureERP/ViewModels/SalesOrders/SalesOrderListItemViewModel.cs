using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.SalesOrders;

public class SalesOrderListItemViewModel
{
    public int SalesOrderId { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime SODate { get; set; }
    public string? Reference { get; set; }
    public SalesOrderStatus Status { get; set; }
}
