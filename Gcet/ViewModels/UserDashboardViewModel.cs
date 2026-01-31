using System;

namespace Gcet.ViewModels
{
    /// <summary>
    /// Information displayed on the end-user dashboard.
    /// </summary>
    public class UserDashboardViewModel
    {
        public string Username { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
    }
}
