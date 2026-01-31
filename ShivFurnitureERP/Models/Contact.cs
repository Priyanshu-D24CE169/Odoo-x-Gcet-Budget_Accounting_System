using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class Contact
{
    public int ContactId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(250)]
    public string? Street { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? Pincode { get; set; }

    public ContactType Type { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public ApplicationUser? PortalUser { get; set; }

    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
}
