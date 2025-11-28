namespace NRLApp.Models
{
    /// <summary>
    /// Viser bruker og deres rolle.
    /// <summary>
    public class AdminUserRoleVm
    {
        
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? CurrentRole { get; set; }
    }

    /// <summary>
    /// Samler brukerne som vises i Adminliste.
    /// <summary>
    public class AdminRoleListVm
    {
        public List<AdminUserRoleVm> Users { get; set; } = new();
    }

    /// <summary>
    /// Endring av bruker sin rolle.
    /// <summary>
    public class AdminUpdateRoleVm
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
