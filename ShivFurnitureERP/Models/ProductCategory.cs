using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class ProductCategory
{
    public int ProductCategoryId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
