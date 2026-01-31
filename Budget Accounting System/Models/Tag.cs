using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Models;

public class Tag
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
}

public class ContactTag
{
    public int ContactId { get; set; }
    public int TagId { get; set; }
    
    public Contact Contact { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
