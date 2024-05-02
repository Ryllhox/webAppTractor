using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductOption> ProductOptions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CommercialOffer> CommercialOffers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //подключения к базе данных PostgreSQL
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=db_web2;Username=postgres;Password=123");
        }
    }
}
