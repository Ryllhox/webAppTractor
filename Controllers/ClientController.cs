using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        public IActionResult Products(int categoryId)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
            {
                return NotFound();
            }

            var products = _context.Products.Where(p => p.CategoryId == categoryId).ToList();
            ViewBag.CategoryName = category.Name; // Передаем название категории в представление
            return View(products);
        }


        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ProductId = id;
            ViewBag.ProductName = product.Name;
            ViewBag.ProductPrice = product.Price;
            ViewBag.ProductOptions = _context.ProductOptions.Where(po => po.ProductId == id).ToList();
            ViewBag.ImagePath = product.ImagePath;

            return View();
        }


        [HttpPost]
        public IActionResult Order(int productId, int[] optionIds)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            // Получаем текущего пользователя
            var user = _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }

            // Получаем выбранные опции
            var options = _context.ProductOptions.Where(o => optionIds.Contains(o.Id)).ToList();
            decimal totalOptionPrice = options.Sum(o => o.Price);

            // Общая цена товара с учетом выбранных опций
            decimal totalPrice = product.Price + totalOptionPrice;

            // Создаем новый заказ
            var order = new Order
            {
                Status = "Pending",
                Date = DateTime.UtcNow,
                Products = product.Name,
                Quantity = 1, // Пока что предположим, что всегда заказывается одна единица товара
                Options = string.Join(",", options.Select(o => o.Name)), // Просто список названий выбранных опций
                TotalPrice = totalPrice,
                CustomerId = user.Id // Ваша логика для определения ID пользователя
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            return RedirectToAction("Index", "Client");
        }

    }
}
