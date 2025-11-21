using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NRLApp.Models.Account;

namespace NRLApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ----------------- LOGIN -----------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["Title"] = "Logg inn";
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                userName: model.Email,
                password: model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Email);

                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Users", "Admin");

                // Naviger til ønsket side etter innlogging
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                // Midlertidig: send til “Obstacle/Area” eller annen hovedside
                return RedirectToAction("Area", "Obstacle");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Kontoen er midlertidig låst.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Feil e-post eller passord.");
            }

            return View(model);
        }

        // ----------------- REGISTER -----------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewData["Title"] = "Registrer deg";
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true // valgfritt i dev
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                TempData["RegisterSuccess"] = "Bruker opprettet. Logg inn for å fortsette.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ----------------- LOGOUT -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
