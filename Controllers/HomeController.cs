using Microsoft.AspNetCore.Mvc;

namespace NRLApp.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
