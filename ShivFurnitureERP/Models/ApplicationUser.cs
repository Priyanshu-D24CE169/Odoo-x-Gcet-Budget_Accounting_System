using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ShivFurnitureERP.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(12, MinimumLength = 6)]
    public string LoginId { get; set; } = string.Empty;

	[StringLength(150)]
	public string? FullName { get; set; }

	public bool MustChangePassword { get; set; } = true;

    public int? ContactId { get; set; }
    public Contact? Contact { get; set; }
}
