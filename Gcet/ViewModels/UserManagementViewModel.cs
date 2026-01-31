using System;

namespace Gcet.ViewModels
{
    /// <summary>
    /// Projection of User entity for admin dashboard to avoid exposing sensitive fields like password hash.
    /// </summary>
    public class UserManagementViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}
