using System.Collections.Generic;

namespace NRLApp.Models
{
/// <summary>
/// Viewmodel for oversiktssiden i adminpanelet. 
/// <summary>
    public class AdminUserListViewModel
    {
        public string? Search { get; set; }
        
        /// <summary>
        /// Viser roller som kan velges i dropdown menyen.
        /// <summary>
        public List<string> AvailableRoles { get; } = new();
        
        /// <summary>
        /// Viser brukere med eksisterende roller.
        /// <summary>
        public List<UserRoleEntry> Users { get; } = new();

        public string? FlashMessage { get; set; }
    }

    public class UserRoleEntry
    {
        public string UserId { get; set; } = default!;

        public string Email { get; set; } = default!;

        /// <summary>
        /// Viser eksisterende roller hentet fra Identity.
        /// <summary>
        public IList<string> CurrentRoles { get; set; } = new List<string>();

        // Rollen som er valgt i dropdown
        public string? SelectedRole { get; set; }
    }
}
