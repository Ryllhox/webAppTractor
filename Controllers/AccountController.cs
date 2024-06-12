using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _userService.Authenticate(email, password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Неверный e-mail или пароль");
                return View();
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var claimsIdentity = new ClaimsIdentity(claims, "login");

            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));

            // Решение, чтобы перенаправлять на соответствующий контроллер в зависимости от роли пользователя
            if (user.Role == "Client")
            {
                return RedirectToAction("Index", "Client");
            }
            else if (user.Role == "Manager")
            {
                return RedirectToAction("Index", "Manager");
            }
            else if (user.Role == "Administrator")
            {
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                // Роль не определена, обработайте соответствующим образом
                return RedirectToAction("Index", "Home");
            }
        }



        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string firstName, string lastName, string patronymic,string phone, string email, string password, string role)
        {
            var user = _userService.Register(firstName, lastName, patronymic, phone, email, password, role);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Данный e-mail уже существует в системе");
                return View();
            }

            // Регистрация успешна, перенаправляем пользователя на страницу входа
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }

    }
}
