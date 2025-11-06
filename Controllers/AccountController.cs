using Microsoft.AspNetCore.Mvc;
using NRLApp.Models;

namespace NRLApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login() => View();

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            // Server-side validering
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Skriv inn e-post og passord.");
                return View(); // <- viser samme view med feilmelding
            }

            // Midlertidig “innlogging”
            bool isAdmin = email.Contains("admin", StringComparison.OrdinalIgnoreCase);
            bool ok = password == "demo123"; // TODO: bytt til ekte sjekk senere

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Feil e-post eller passord.");
                return View();
            }

            return isAdmin
                ? RedirectToAction("Dashboard", "Admin")    // stub
                : RedirectToAction("Area", "Obstacle");     // pilot
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // TODO: lagre bruker senere (Identity/DB)
            TempData["RegisterSuccess"] = "Konto opprettet. Logg inn for å fortsette.";

            return RedirectToAction("Login");
        }
    }
}


