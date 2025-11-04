using Microsoft.AspNetCore.Mvc;
using NRLApp.Models;

namespace NRLApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            // TODO: Koble på ekte autentisering senere.
            // Midlertidig: send brukeren videre til skjemaet.
            return RedirectToAction("Area", "Obstacle");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Viser valideringsfeil tilbake i skjemaet
                return View(model);
            }

            // TODO: Lagre bruker med Identity senere.
            TempData["RegisterSuccess"] = "Konto opprettet! Du kan nå logge inn.";
            return RedirectToAction("Login");
        }
    }
}

