using System.ComponentModel.DataAnnotations;

namespace Gcet.ViewModels
{
    /// <summary>
    /// Data used by administrators to create managed accounts.
    /// </summary>
    public class CreateUserViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be Admin or User.")]
        public string Role { get; set; } = "User";
    }
}
