using System.Collections.Generic;

namespace Gcet.ViewModels
{
    /// <summary>
    /// Container for paginated user management results.
    /// </summary>
    public class UsersListViewModel
    {
        public IEnumerable<UserManagementViewModel> Users { get; set; } = new List<UserManagementViewModel>();
        public string? Search { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
