using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers
{
    public class ErrorController : Controller
    {

        [AllowAnonymous]
        [Route("/Error")]
        public IActionResult Error()
        {
            return View();
        }

        [AllowAnonymous]
        [Route("/Error/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
