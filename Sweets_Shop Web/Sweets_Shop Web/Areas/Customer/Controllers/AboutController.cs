using Microsoft.AspNetCore.Mvc;

namespace Sweets_Shop_Web.Areas.Customer.Controllers
{
    [Area("Customer")]


    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
