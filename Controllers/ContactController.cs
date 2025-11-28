using Microsoft.AspNetCore.Mvc;

namespace NRLApp.Controllers
{
/// <summary>
/// Controller til kontaktsiden.
/// <summary>
    public class ContactController : Controller
    {
    /// <summary>
    /// Returnerer kontaktsiden uten modell-data
    /// <summary>
        public IActionResult Index()
        {
            return View();
        }
    }
}
