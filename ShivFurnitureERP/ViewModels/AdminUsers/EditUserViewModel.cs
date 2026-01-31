using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShivFurnitureERP.ViewModels.AdminUsers;

public class EditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Login ID")]
    public string LoginId { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    [Display(Name = "Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email ID")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = "PortalUser";

    [Display(Name = "Require password change on next login")]
    public bool MustChangePassword { get; set; }

    public IEnumerable<SelectListItem> AvailableRoles { get; set; } = Enumerable.Empty<SelectListItem>();
}
