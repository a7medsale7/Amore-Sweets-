using Sweet_Shop.Data;
using Sweets.Models;
using Sweets.Models.Models;

namespace Sweet_Shop.Repository
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Category> Category {  get; }
        IGenericRepository<Product> Product { get; }
        IGenericRepository<ShoppingCart> ShoppingCart { get; }
        IGenericRepository<ApplicationUser> ApplicationUser { get; }
        IGenericRepository<WishList> WishList { get; }
        IGenericRepository<OrderHeader> OrderHeader { get; }
        IGenericRepository<OrderDetails> OrderDetails { get; }
        IGenericRepository<ProductImage> ProductImage { get; }
        IGenericRepository<Feedback> Feedback { get; }
        void save();


    }
}
