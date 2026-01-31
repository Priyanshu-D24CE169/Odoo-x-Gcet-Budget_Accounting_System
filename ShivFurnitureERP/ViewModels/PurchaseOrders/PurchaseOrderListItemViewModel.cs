using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.PurchaseOrders;

public class PurchaseOrderListItemViewModel
{
    public int PurchaseOrderId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime PODate { get; set; }
    public string? Reference { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
}
