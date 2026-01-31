using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShivFurnitureERP.Models;

public class Product
{
    public int ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int ProductCategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal SalesPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal PurchasePrice { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
