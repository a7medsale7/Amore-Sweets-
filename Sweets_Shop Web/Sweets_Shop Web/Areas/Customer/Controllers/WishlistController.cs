using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sweet_Shop.Repository;
using Sweets.Models.Models;
using Sweets.Utility;
using System.Security.Claims;

namespace Sweets_Shop_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public WishlistController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ✅ عرض العناصر في الـ Wishlist
        public IActionResult Index()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = _unitOfWork.WishList.GetAll(
                w => w.ApplicationUserId == userId,
                            includeProperties: "Product,Product.ProductImages" // ✅ جلب الصور
);

            return View(items);
        }

        // ✅ إضافة أو إزالة منتج (Toggle)
        [HttpPost]
        public IActionResult Toggle(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = _unitOfWork.WishList.Get(
                w => w.ApplicationUserId == userId && w.ProductId == productId);

            if (existing == null)
            {
                var wish = new WishList
                {
                    ProductId = productId,
                    ApplicationUserId = userId,
                    AddedAt = DateTime.UtcNow
                };
                _unitOfWork.WishList.Add(wish);
                TempData["success"] = "Added to wishlist";
            }
            else
            {
                _unitOfWork.WishList.Remove(existing);
                TempData["info"] = "Removed from wishlist";
            }

            _unitOfWork.save();
            HttpContext.Session.SetInt32(SD.WishlistCount,
    _unitOfWork.WishList.GetAll(w => w.ApplicationUserId == userId).Count());

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        // ✅ إزالة منتج يدويًا من الصفحة
        [HttpPost]
        public IActionResult Remove(int id)
        {
            var item = _unitOfWork.WishList.Get(w => w.Id == id);
            if (item != null)
            {
                _unitOfWork.WishList.Remove(item);
                _unitOfWork.save();
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // ✅ حدّث الكاونتر بعد الحذف
                HttpContext.Session.SetInt32(SD.WishlistCount,
                    _unitOfWork.WishList.GetAll(c => c.ApplicationUserId == userId).Count());

                TempData["success"] = "Removed from wishlist";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ نقل المنتج من Wishlist إلى Cart
        [HttpPost]
        public IActionResult MoveToCart(int wishlistId)
        {
            var wish = _unitOfWork.WishList.Get(
                w => w.Id == wishlistId, includeProperties: "Product");

            if (wish == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartFromDb = _unitOfWork.ShoppingCart.Get(
                c => c.ApplicationUserId == userId && c.ProductId == wish.ProductId);

            if (cartFromDb == null)
            {
                var cart = new ShoppingCart
                {
                    ProductId = wish.ProductId,
                    Count = 1,
                    ApplicationUserId = userId
                };
                _unitOfWork.ShoppingCart.Add(cart);
            }
            else
            {
                cartFromDb.Count += 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }

            // إزالة المنتج من الـ Wishlist بعد النقل
            _unitOfWork.WishList.Remove(wish);
            _unitOfWork.save();
            HttpContext.Session.SetInt32(SD.SessionCart,
               _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId).Count());
            HttpContext.Session.SetInt32(SD.WishlistCount,
                _unitOfWork.WishList.GetAll(w => w.ApplicationUserId == userId).Count());

            TempData["success"] = "Moved to cart successfully!";
            return RedirectToAction("Index", "Cart", new { area = "Customer" });
        }
    }
}
