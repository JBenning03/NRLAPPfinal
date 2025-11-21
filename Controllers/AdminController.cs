using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NRLApp.Models;

namespace NRLApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private static readonly string[] AssignableRoles = new[] { "Pilot", "Crew", "Approver" };

        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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

            foreach (var role in AssignableRoles)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                    await _userManager.RemoveFromRoleAsync(user, role);
            }

            if (!await _roleManager.RoleExistsAsync(vm.Role))
                await _roleManager.CreateAsync(new IdentityRole(vm.Role));

            await _userManager.AddToRoleAsync(user, vm.Role);

            TempData["Status"] = $"Oppdatert rolle for {user.Email ?? user.UserName} til {vm.Role}.";
            return RedirectToAction(nameof(Users));
        }
    }
}