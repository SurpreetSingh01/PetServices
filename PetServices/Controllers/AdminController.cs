using Microsoft.AspNetCore.Mvc;

namespace PetServices.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
