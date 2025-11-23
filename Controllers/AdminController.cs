using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NRLApp.Models;

namespace NRLApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private static readonly string[] AssignableRoles = new[] { "Pilot", "Crew", "Approver" };

        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        public AdminController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        private MySqlConnection CreateConnection()
            => new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        private sealed class OrgRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        private sealed class UserOrgRow
        {
            public string UserId { get; set; } = "";
            public int OrganizationId { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var vm = new AdminRoleListVm();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var currentRole = AssignableRoles.FirstOrDefault(r => roles.Contains(r));

                vm.Users.Add(new AdminUserRoleVm
                {
                    UserId = user.Id,
                    Email = user.Email ?? user.UserName,
                    CurrentRole = currentRole
                });
            }

            // --- HENT ORGANISASJONER STERKT TYPET ---
            await using (var con = new MySqlConnector.MySqlConnection(
                             _config.GetConnectionString("DefaultConnection")))
            {
                var orgs = await con.QueryAsync<OrganizationVm>(@"
            SELECT 
                id   AS Id,
                name AS Name
            FROM organizations
            ORDER BY name;");

                ViewBag.Organizations = orgs;

                var userOrgRows = await con.QueryAsync<(string UserId, int OrganizationId)>(@"
            SELECT user_id   AS UserId,
                   organization_id AS OrganizationId
            FROM user_organizations;");

                var userOrgMap = userOrgRows.ToDictionary(x => x.UserId, x => x.OrganizationId);
                ViewBag.UserOrgMap = userOrgMap;
            }

            ViewBag.AssignableRoles = AssignableRoles;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(AdminUpdateRoleVm vm)
        {
            if (!AssignableRoles.Contains(vm.Role))
            {
                ModelState.AddModelError(string.Empty, "Ugyldig rolle valgt.");
                return await Users();
            }

            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null)
            {
                TempData["Error"] = "Fant ikke bruker.";
                return RedirectToAction(nameof(Users));
            }

            // 1) Oppdater rolle
            foreach (var role in AssignableRoles)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                    await _userManager.RemoveFromRoleAsync(user, role);
            }

            if (!await _roleManager.RoleExistsAsync(vm.Role))
                await _roleManager.CreateAsync(new IdentityRole(vm.Role));

            await _userManager.AddToRoleAsync(user, vm.Role);

            // 2) Oppdater organisasjon (fra form-feltet "OrganizationId")
            int? orgId = null;
            var orgValue = Request.Form["OrganizationId"].FirstOrDefault();
            if (int.TryParse(orgValue, out var parsed))
            {
                orgId = parsed;
            }

            using (var con = CreateConnection())
            {
                if (orgId.HasValue)
                {
                    const string upsertSql = @"
INSERT INTO user_organizations (user_id, organization_id)
VALUES (@UserId, @OrgId)
ON DUPLICATE KEY UPDATE organization_id = VALUES(organization_id);";

                    await con.ExecuteAsync(upsertSql, new
                    {
                        UserId = user.Id,
                        OrgId = orgId.Value
                    });
                }
                else
                {
                    const string deleteSql = "DELETE FROM user_organizations WHERE user_id = @UserId;";
                    await con.ExecuteAsync(deleteSql, new { UserId = user.Id });
                }
            }

            TempData["Status"] = $"Oppdatert rolle og organisasjon for {user.Email ?? user.UserName}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Fant ikke bruker.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Klarte ikke å slette bruker: " +
                                    string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            using (var con = CreateConnection())
            {
                await con.ExecuteAsync(
                    "DELETE FROM user_organizations WHERE user_id = @UserId;",
                    new { UserId = userId });
            }

            TempData["Status"] = $"Bruker {user.Email ?? user.UserName} er slettet.";
            return RedirectToAction(nameof(Users));
        }
    }
}
