namespace Gcet.ViewModels
{
    /// <summary>
    /// Snapshot of key user metrics for administrators.
    /// </summary>
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int LockedUsers { get; set; }
    }
}
