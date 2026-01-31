namespace ShivFurnitureERP.ViewModels.Products;

public class ProductListItemViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal SalesPrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedOn { get; set; }
}
