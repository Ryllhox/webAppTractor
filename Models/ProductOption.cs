﻿namespace WebApplication2.Models
{
    public class ProductOption
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
