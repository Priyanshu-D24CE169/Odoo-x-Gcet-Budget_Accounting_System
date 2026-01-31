using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.ViewModels;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Login ID or Email")]
    [StringLength(256, MinimumLength = 6)]
    public string LoginId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
