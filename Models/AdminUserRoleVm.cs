namespace NRLApp.Models
{
    public class AdminUserRoleVm
    {
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? CurrentRole { get; set; }
    }

    public class AdminRoleListVm
    {
        public List<AdminUserRoleVm> Users { get; set; } = new();
    }

    public class AdminUpdateRoleVm
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}