using System.Collections.Generic;

namespace NRLApp.Models
{
    public class AdminUserListViewModel
    {
        public string? Search { get; set; }

        public List<string> AvailableRoles { get; } = new();

        public List<UserRoleEntry> Users { get; } = new();

        public string? FlashMessage { get; set; }
    }

    public class UserRoleEntry
    {
        public string UserId { get; set; } = default!;

        public string Email { get; set; } = default!;

        public IList<string> CurrentRoles { get; set; } = new List<string>();

        // Rollen som er valgt i dropdown
        public string? SelectedRole { get; set; }
    }
}
