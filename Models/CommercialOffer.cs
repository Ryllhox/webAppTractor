namespace WebApplication2.Models
{
    public class CommercialOffer
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalPrice { get; set; }
        public int OrderId { get; set; }
    }
}
