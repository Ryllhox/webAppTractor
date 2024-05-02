namespace WebApplication2.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Products { get; set; } // Это можно изменить на коллекцию отдельных продуктов
        public int Quantity { get; set; }
        public string Options { get; set; } // Также можно изменить на коллекцию
        public decimal TotalPrice { get; set; }
        public int CustomerId { get; set; }
    }
}
