using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShivFurnitureERP.ViewModels.AdminUsers;

public class CreateUserViewModel
{
    [Required]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be between {2} and {1} characters.")]
    [Display(Name = "Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Login ID")]
    [RegularExpression("^[A-Za-z0-9]{6,12}$", ErrorMessage = "Login ID must be 6-12 characters and contain only letters or numbers.")]
    public string LoginId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email ID")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = "PortalUser";

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [StringLength(100, MinimumLength = 9, ErrorMessage = "Password must be at least {2} characters long.")]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+\\-=`~{}\\[\\]|:\";'<>?,./]).{9,}$", ErrorMessage = "Password must include upper, lower, and special characters.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Re-enter password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> AvailableRoles { get; set; } = Enumerable.Empty<SelectListItem>();
}
