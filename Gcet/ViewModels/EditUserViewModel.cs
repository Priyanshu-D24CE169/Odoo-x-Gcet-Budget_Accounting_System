using System.ComponentModel.DataAnnotations;

namespace Gcet.ViewModels
{
    /// <summary>
    /// Form values for editing an existing managed user.
    /// </summary>
    public class EditUserViewModel
    {
        [Required]
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be Admin or User.")]
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; }
    }
}
