namespace Gcet.Services
{
    /// <summary>
    /// Aggregated user counts for the admin dashboard.
    /// </summary>
    public class UserStatistics
    {
        public int TotalUsers { get; init; }
        public int ActiveUsers { get; init; }
        public int InactiveUsers { get; init; }
        public int LockedUsers { get; init; }
    }
}
