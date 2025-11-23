using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace NRLApp.Controllers
{
/// <summary>
/// Viser Homepage for innloggende brukere.
/// <summary>
    [Authorize]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
