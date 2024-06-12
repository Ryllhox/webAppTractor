using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using WebApplication2.Models;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ManagerController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var orders = _context.Orders.ToList();
            var customersIDs = orders.Select(o => o.CustomerId).Distinct().ToList();
            var customers = _context.Users.Where(u => customersIDs.Contains(u.Id)).ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
            ViewBag.Orders = orders; // установите список заказов в ViewBag
            ViewBag.Customers = customers;
            return View();
        }

        public IActionResult Orders(string status)
        {
            IQueryable<Order> ordersQuery = _context.Orders;

            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var orders = ordersQuery.ToList();

            // Загрузка данных о клиентах для каждого заказа
            var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
            var customers = _context.Users
                .Where(u => customerIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}"); // Сохраняем ФИО в словаре

            ViewBag.Status = status;
            ViewBag.Orders = orders;
            ViewBag.Customers = customers;

            return View("Index"); // Исправлено, чтобы возвращать представление "Index"
        }


        public IActionResult CreateOffer(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Проверяем, есть ли уже оффер для этого заказа
            var existingOffer = _context.CommercialOffers.FirstOrDefault(o => o.OrderId == id);
            if (existingOffer != null)
            {
                // Если уже есть оффер, просто возвращаемся обратно
                return RedirectToAction("Index");
            }

            // Получаем информацию о пользователе
            var customer = _context.Users.FirstOrDefault(u => u.Id == order.CustomerId);
            // Создаем документ Word с информацией по заказу
            var doc = CreateOfferDocument(order, customer);
                     
            // Сохраняем информацию об оффере в базу данных
            var offer = new CommercialOffer
            {
                Date = DateTime.UtcNow,
                TotalPrice = order.TotalPrice,
                OrderId = order.Id
            };

            _context.CommercialOffers.Add(offer);
            order.Status = "Выполнено";
            _context.SaveChanges();

            // Создаем поток в памяти для хранения документа
            using (var memoryStream = new MemoryStream())
            {
                // Сохраняем документ в поток
                doc.SaveAs(memoryStream);

                // Устанавливаем позицию потока на начало
                memoryStream.Position = 0;

                // Возвращаем файл клиенту
                string fileName = $"Offer_{order.Id}.docx";
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
            }


            return RedirectToAction("Index", "Manager");
        }

        // Метод для создания документа Word с информацией по заказу
        private DocX CreateOfferDocument(Order order, User customer)
        {
            DocX doc = DocX.Create(Path.Combine(Directory.GetCurrentDirectory(), "temp.docx"));

            // Заголовок документа
            doc.InsertParagraph($"Коммерческое предложение для Заказа #{order.Id}")
                .FontSize(16d)
                .Bold()
                .Alignment = Alignment.center;

            // Информация о заказе
            doc.InsertParagraph($"Дата: {order.Date.ToShortDateString()}")
                .FontSize(12d)
                .Bold();
            doc.InsertParagraph($"КОД заказа: {order.Id}")
                .FontSize(12d);

            // Информация о покупателе
            doc.InsertParagraph($"Имя: {customer.FirstName} {customer.LastName} {customer.Patronymic}")
                .FontSize(12d);
            doc.InsertParagraph($"Телефон: {customer.Phone}")
                .FontSize(12d);
            doc.InsertParagraph($"Электронная почта: {customer.Email}")
                .FontSize(12d);

            // Таблица с информацией о продуктах и фотографиями
            Table productsTable = doc.AddTable(1, 6); // Увеличиваем количество столбцов до 6
            productsTable.Design = TableDesign.LightShadingAccent1;
            productsTable.Alignment = Alignment.left;
            productsTable.AutoFit = AutoFit.Contents;
            productsTable.Rows[0].Cells[0].Paragraphs.First().Append("Фотография"); // Добавляем столбец для фотографий
            productsTable.Rows[0].Cells[1].Paragraphs.First().Append("Наименование");
            productsTable.Rows[0].Cells[2].Paragraphs.First().Append("Количество");
            productsTable.Rows[0].Cells[3].Paragraphs.First().Append("Цена за единицу");
            productsTable.Rows[0].Cells[4].Paragraphs.First().Append("Опции");
            productsTable.Rows[0].Cells[5].Paragraphs.First().Append("Общая цена");

            // Заполнение таблицы
            var productsAndQuantities = order.Products.Split(';');
            decimal totalPrice = 0;
            for (int i = 0; i < productsAndQuantities.Length; i++)
            {
                string productAndQuantity = productsAndQuantities[i];
                string[] productInfo = productAndQuantity.Split(':');
                string productName = productInfo[0].Trim();
                int quantity = 1;
                if (productInfo.Length > 1)
                {
                    quantity = int.Parse(productInfo[1].Trim());
                }

                // Находим продукт и его цену
                var product = _context.Products.FirstOrDefault(p => p.Name == productName);
                decimal productPrice = product?.Price ?? 0;

                // Находим опции и их цены
                var options = order.Options.Split(';')[i].Split(',');
                decimal optionsTotalPrice = 0;
                string optionsList = string.Join(", ", options.Select(o => $"{o} ({_context.ProductOptions.FirstOrDefault(po => po.Name == o)?.Price ?? 0} руб.)"));
                foreach (string option in options)
                {
                    decimal optionPrice = _context.ProductOptions.FirstOrDefault(po => po.Name == option)?.Price ?? 0;
                    optionsTotalPrice += optionPrice;
                }

                // Вычисляем общую цену для продукта с учетом опций
                decimal productTotalPrice = (productPrice + optionsTotalPrice) * quantity;
                totalPrice += productTotalPrice;

                // Добавляем строку с данными о товаре
                productsTable.InsertRow();
                var newRow = productsTable.Rows[i + 1];
                newRow.Cells[1].Paragraphs.First().Append(productName);
                newRow.Cells[2].Paragraphs.First().Append(quantity.ToString());
                newRow.Cells[3].Paragraphs.First().Append(productPrice.ToString("F2"));
                newRow.Cells[4].Paragraphs.First().Append(optionsList);
                newRow.Cells[5].Paragraphs.First().Append(productTotalPrice.ToString("F2"));

                // Добавляем фотографию товара
                if (product?.ImagePath != null && System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, product.ImagePath.TrimStart('/'))))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImagePath.TrimStart('/'));
                    var picture = doc.AddImage(imagePath).CreatePicture(100, 100); // Задаем размеры изображения
                    newRow.Cells[0].Paragraphs.First().AppendPicture(picture);
                }
                else
                {
                    newRow.Cells[0].Paragraphs.First().Append("Нет изображения");
                }
            }

            doc.InsertTable(productsTable);

            // Итоговая цена
            doc.InsertParagraph($"Итоговая цена: {totalPrice.ToString("F2")} руб.")
                .FontSize(12d);

            return doc;
        }


        public IActionResult RejectOrder(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = "Отклонено";
            _context.SaveChanges();

            return RedirectToAction("Index", "Manager");
        }

        public IActionResult MarkAsProcessing(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = "В процессе";
            _context.SaveChanges();

            return RedirectToAction("Index", "Manager");
        }

    }
}
