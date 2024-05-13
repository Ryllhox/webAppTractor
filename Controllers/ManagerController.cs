using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var orders = _context.Orders.ToList();
            ViewBag.Orders = orders; // установите список заказов в ViewBag
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
            var customers = _context.Users.Where(u => customerIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u);

            ViewBag.Status = status;
            ViewBag.Orders = orders;
            ViewBag.Customers = customers;

            return View("~/Views/Manager/Index.cshtml");
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

            // Создаем документ Word с информацией по заказу
            var doc = CreateOfferDocument(order);

            // Создаем папку для сохранения файла в папке загрузки, если она не существует
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloadFolder = Path.Combine(folderPath, "Downloads");

            // Сохраняем документ в файл
            string fileName = $"Offer_{order.Id}.docx";
            string filePath = Path.Combine(downloadFolder, fileName);
            doc.SaveAs(filePath);

            // Сохраняем информацию об оффере в базу данных
            var offer = new CommercialOffer
            {
                Date = DateTime.UtcNow,
                TotalPrice = order.TotalPrice,
                OrderId = order.Id
            };

            _context.CommercialOffers.Add(offer);
            order.Status = "Processed";
            _context.SaveChanges();

            return RedirectToAction("Index", "Manager");
        }

        // Метод для создания документа Word с информацией по заказу
        private DocX CreateOfferDocument(Order order)
        {
            DocX doc = DocX.Create(Path.Combine(Directory.GetCurrentDirectory(), "temp.docx"));

            // Заголовок документа
            doc.InsertParagraph($"Commercial Offer for Order #{order.Id}")
                .FontSize(16d)
                .Bold()
                .Alignment = Alignment.center;

            // Информация о заказе
            doc.InsertParagraph($"Date: {DateTime.UtcNow}")
                .FontSize(12d)
                .Bold();
            doc.InsertParagraph($"Order ID: {order.Id}")
                .FontSize(12d);
            doc.InsertParagraph($"Status: {order.Status}")
                .FontSize(12d);

            // Таблица с информацией о продуктах
            Table productsTable = doc.AddTable(1, 4);
            productsTable.Design = TableDesign.LightShadingAccent1;
            productsTable.Alignment = Alignment.left;
            productsTable.AutoFit = AutoFit.Contents;
            productsTable.Rows[0].Cells[0].Paragraphs.First().Append("Product");
            productsTable.Rows[0].Cells[1].Paragraphs.First().Append("Quantity");
            productsTable.Rows[0].Cells[2].Paragraphs.First().Append("Options");
            productsTable.Rows[0].Cells[3].Paragraphs.First().Append("Total Price");

            // Заполнение таблицы
            string[] products = order.Products.Split(';');
            string[] quantities = order.Quantity.ToString().Split(';');
            string[] options = order.Options.Split(';');
            string[] prices = order.TotalPrice.ToString().Split(';');
            for (int i = 0; i < products.Length; i++)
            {
                productsTable.InsertRow();
                productsTable.Rows[i + 1].Cells[0].Paragraphs.First().Append(products[i]);
                productsTable.Rows[i + 1].Cells[1].Paragraphs.First().Append(quantities[i]);
                productsTable.Rows[i + 1].Cells[2].Paragraphs.First().Append(options[i]);
                productsTable.Rows[i + 1].Cells[3].Paragraphs.First().Append(prices[i]);
            }

            doc.InsertTable(productsTable);

            // Итоговая цена
            doc.InsertParagraph($"Total Price: {order.TotalPrice}")
                .FontSize(12d);
            doc.InsertParagraph($"Customer: {order.CustomerId}")
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

            order.Status = "Rejected";
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

            order.Status = "Processing";
            _context.SaveChanges();

            return RedirectToAction("Index", "Manager");
        }

    }
}
