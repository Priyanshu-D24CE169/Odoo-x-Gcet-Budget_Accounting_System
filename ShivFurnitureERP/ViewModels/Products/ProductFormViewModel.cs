using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.ViewModels.Products;

public class ProductFormViewModel
{
    public int? ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category")]
    [MaxLength(150)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    [Display(Name = "Sales Price")]
    public decimal SalesPrice { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Purchase Price")]
    public decimal PurchasePrice { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
