using Microsoft.AspNetCore.Mvc;
using Sweet_Shop.Repository;
using Sweets.Models;

namespace Sweets_Shop_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            // ✅ جلب كل المنتجات مع الفئة والصور المرتبطة بها
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages");
            return View(productList);
        }
    }
}
