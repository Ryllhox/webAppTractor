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

        public IActionResult Products(int categoryId, decimal? minPrice, decimal? maxPrice, int? minPower, int? maxPower)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
            {
                return NotFound();
            }

            // Фильтрация продуктов по категории
            var products = _context.Products.Where(p => p.CategoryId == categoryId);

            // Вычисление минимальной и максимальной цены
            decimal minProductPrice = products.Min(p => p.Price);
            decimal maxProductPrice = products.Max(p => p.Price);

            // Вычисление минимальной и максимальной мощности
            int minProductPower = products.Min(p => p.Power);
            int maxProductPower = products.Max(p => p.Power);

            // Фильтрация по цене
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice);
            }

            // Фильтрация по мощности
            if (minPower.HasValue)
            {
                products = products.Where(p => p.Power >= minPower);
            }
            if (maxPower.HasValue)
            {
                products = products.Where(p => p.Power <= maxPower);
            }

            ViewBag.CategoryName = category.Name;
            ViewBag.MinProductPrice = minProductPrice;
            ViewBag.MaxProductPrice = maxProductPrice;
            ViewBag.MinProductPower = minProductPower;
            ViewBag.MaxProductPower = maxProductPower;
            ViewBag.CurrentCategoryId = categoryId;

            return View(products.ToList());
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
            ViewBag.Power = product.Power;
            ViewBag.ProductOptions = _context.ProductOptions.Where(po => po.ProductId == id).ToList();
            ViewBag.ImagePath = product.ImagePath;

            return View();
        }


        [HttpPost]
        public IActionResult Order(int productId, string selectedOptionIds)
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

            // Преобразуем строку selectedOptionIds в массив целых чисел
            var optionIds = new List<int>();
            if (!string.IsNullOrEmpty(selectedOptionIds))
            {
                optionIds = selectedOptionIds.Split(',').Select(int.Parse).ToList();
            }

            // Получаем выбранные опции
            var options = _context.ProductOptions.Where(o => optionIds.Contains(o.Id)).ToList();
            decimal totalOptionPrice = options.Sum(o => o.Price);

            // Общая цена товара с учетом выбранных опций
            decimal totalPrice = product.Price + totalOptionPrice;

            // Создаем новый заказ
            var order = new Order
            {
                Status = "Обработка",
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
