using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
