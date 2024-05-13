namespace WebApplication2.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Power { get; set; }
        public bool Availability { get; set; }
        public string? ImagePath { get; set; }
    }
}
