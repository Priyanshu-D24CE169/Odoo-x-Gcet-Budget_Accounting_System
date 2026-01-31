using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class Tag
{
    public int TagId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
}
