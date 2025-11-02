using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sweets.Models;
using Sweets.Models.Models;


namespace Sweet_Shop.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<WishList> WishLists { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }

        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Feedback> feedbacks { get; set; }
        public DbSet<Notification> Notifications { get; set; }





        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Slug لازم Unique
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            // Name لازم Unique
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();
            // Slug لازم Unique
            modelBuilder.Entity<Product>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            // Name لازم Unique
            modelBuilder.Entity<Product>()
                .HasIndex(c => c.Name)
                .IsUnique();

        }
    }
}