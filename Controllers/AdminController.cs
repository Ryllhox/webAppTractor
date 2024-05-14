using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Страница со списком пользователей
        public IActionResult Users()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // Страница для редактирования пользователя
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Update(user);
                _context.SaveChanges();
                return RedirectToAction("Users");
            }
            return View(user);
        }

        // Страница для удаления пользователя
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            _context.Users.Remove(user);
            _context.SaveChanges();
            return RedirectToAction("Users");
        }

        // Страница со списком продуктов
        public IActionResult Products()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        // Страница для добавления продукта
        [HttpGet]
        public IActionResult AddProduct()
        {
            var categories = _context.Categories.ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct([Bind("Id,CategoryId,Name,Price,Power,Availability")] Product product, IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                string uniqueFileName = UploadImage(imageFile);
                if (uniqueFileName != null)
                {
                    product.ImagePath = Path.Combine("/images", uniqueFileName);
                }
                else
                {
                    ModelState.AddModelError("imageFile", "Failed to upload image.");
                }
            }
            else
            {
                ModelState.AddModelError("imageFile", "Please select an image to upload.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction("Products", "Admin");
            }
            return View(product);
        }



        private string UploadImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", uniqueFileName);

            // Создать папку "images", если она не существует
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }

            return uniqueFileName;
        }


        // Страница для редактирования продукта
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var categories = _context.Categories.ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            // Передаем текущий путь к изображению в ViewBag
            ViewBag.ImagePath = product.ImagePath;

            return View(product);
        }

        [HttpPost]
        public IActionResult EditProduct(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uniqueFileName = UploadImage(imageFile);
                    if (uniqueFileName != null)
                    {
                        // Удаляем старое изображение, если оно существует
                        if (!string.IsNullOrEmpty(product.ImagePath))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", product.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        product.ImagePath = Path.Combine("/images", uniqueFileName);
                    }
                    else
                    {
                        ModelState.AddModelError("imageFile", "Failed to upload image.");
                    }
                }

                _context.Update(product);
                _context.SaveChanges();

                return RedirectToAction("Products", "Admin");
            }

            var categories = _context.Categories.ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);

            // Передаем текущий путь к изображению в ViewBag
            ViewBag.ImagePath = product.ImagePath;

            return View(product);
        }




        // Страница для удаления продукта
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            _context.Products.Remove(product);
            _context.SaveChanges();
            return RedirectToAction("Products");
        }
    }
}
