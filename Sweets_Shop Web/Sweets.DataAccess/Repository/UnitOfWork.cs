using Microsoft.EntityFrameworkCore.Storage;
using Sweet_Shop.Data;
using Sweet_Shop.DataAccess.Data;
using Sweets.Models;
using Sweets.Models.Models;

namespace Sweet_Shop.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
       public IGenericRepository<Category> Category { get; private set; }
        public IGenericRepository<Product> Product { get; private set; }
        public IGenericRepository<ApplicationUser> ApplicationUser { get; private set; }
        public IGenericRepository<ShoppingCart> ShoppingCart { get; private set; }
         public IGenericRepository<WishList> WishList { get; private set; }
        public IGenericRepository<OrderHeader> OrderHeader { get; private set; }
        public IGenericRepository<OrderDetails> OrderDetails { get; private set; }
        public IGenericRepository<ProductImage> ProductImage { get; private set; }
        public IGenericRepository<Feedback> Feedback { get; private set; }
        public IGenericRepository<Notification> Notification { get; private set; }
        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Category = new GenericRepository<Category>(_db);
            Product = new GenericRepository<Product>(_db);
            ApplicationUser = new GenericRepository<ApplicationUser>(_db);
            ShoppingCart = new GenericRepository<ShoppingCart>(_db);
            WishList = new GenericRepository<WishList>(_db);
            OrderHeader = new GenericRepository<OrderHeader>(_db);
            OrderDetails = new GenericRepository<OrderDetails>(_db);
            ProductImage = new GenericRepository<ProductImage>(_db);
            Feedback = new GenericRepository<Feedback>(_db);
            Notification = new GenericRepository<Notification>(_db);
        }


        public void save()
        {
            _db.SaveChanges();
        }
        public void Dispose()
        {
            _db.Dispose();
        }
        public IDbContextTransaction BeginTransaction()
        {
            return _db.Database.BeginTransaction();
        }

    }
}
