using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace NRLApp.Controllers
{
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
