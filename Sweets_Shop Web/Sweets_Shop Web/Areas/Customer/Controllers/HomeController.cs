using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sweet_Shop.Repository;
using Sweets.Models;
using Sweets.Models.Models;
using Sweets.Models.ViewData;
using Sweets.Utility;
using System.Diagnostics;
using System.Security.Claims;

namespace Sweets_Shop_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var cartCount = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();
                    HttpContext.Session.SetInt32(SD.SessionCart, cartCount);

                    var wishlistCount = _unitOfWork.WishList.GetAll(w => w.ApplicationUserId == userId).Count();
                    HttpContext.Session.SetInt32(SD.WishlistCount, wishlistCount);
                }
            }

            var categories = _unitOfWork.Category.GetAll(includeProperties: "Products,Products.ProductImages").ToList();

            var feedbacks = _unitOfWork.Feedback.GetAll(f => f.IsApproved)
                                                .OrderByDescending(f => f.CreatedAt)
                                                .Take(10)
                                                .ToList();

            ViewBag.Feedbacks = feedbacks;

            return View(categories);
        }

        public IActionResult Category(string slug)
        {
            var category = _unitOfWork.Category.Get(
                c => c.Slug == slug,
                includeProperties: "Products,Products.ProductImages"
            );

            if (category == null)
                return NotFound();

            ViewBag.CategorySlug = slug; // نستخدمه لإرجاع المستخدم بعد الإضافة
            return View(category);
        }

        public IActionResult Details(string slug)
        {
            var product = _unitOfWork.Product.Get(
                p => p.Slug == slug,
                includeProperties: "Category,ProductImages"
            );

            if (product == null)
                return NotFound();

            ShoppingCart cart = new()
            {
                Count = 1,
                ProductId = product.Id,
                Product = product
            };

            ViewBag.CategorySlug = product.Category?.Slug; // نرسل الفئة للـ View
            return View(cart);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Details(ShoppingCart shoppingCart, string categorySlug)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return RedirectToAction("Login", "Account", new { area = "Identity" });

            shoppingCart.ApplicationUserId = userId;

            var cartfromdb = _unitOfWork.ShoppingCart.Get(
                c => c.ApplicationUserId == userId && c.ProductId == shoppingCart.ProductId
            );

            if (cartfromdb == null)
            {
                _unitOfWork.ShoppingCart.Add(shoppingCart);
            }
            else
            {
                cartfromdb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartfromdb);
            }

            _unitOfWork.save();

            HttpContext.Session.SetInt32(SD.SessionCart,
                _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId).Count());

            TempData["success"] = "Product added to your cart!";

            // 🔹 دائمًا ارجع لصفحة Category
            // داخل أي Action بتعمل Redirect لصفحة Category
            return RedirectToAction("Category", "Home", new { area = "Customer", slug = categorySlug });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult AddToWishList(int productId, string categorySlug)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return RedirectToAction("Login", "Account", new { area = "Identity" });

            var existingWish = _unitOfWork.WishList.Get(
                w => w.ApplicationUserId == userId && w.ProductId == productId
            );

            if (existingWish == null)
            {
                WishList wishList = new WishList
                {
                    ProductId = productId,
                    ApplicationUserId = userId,
                    AddedAt = DateTime.UtcNow
                };

                _unitOfWork.WishList.Add(wishList);
                _unitOfWork.save();

                HttpContext.Session.SetInt32(SD.WishlistCount,
                    _unitOfWork.WishList.GetAll(w => w.ApplicationUserId == userId).Count());

                TempData["success"] = "Product added to your wishlist 💖";
            }
            else
            {
                TempData["info"] = "This product is already in your wishlist!";
            }

            // 🔹 دائمًا ارجع لصفحة Category
            // داخل أي Action بتعمل Redirect لصفحة Category
            return RedirectToAction("Category", "Home", new { area = "Customer", slug = categorySlug });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Special()
        {
            var specialIds = Sweets.Utility.SpecialProductsCache.GetAll();

            var specialProducts = _unitOfWork.Product
                .GetAll(includeProperties: "Category,ProductImages")
                .Where(p => specialIds.Contains(p.Id))
                .ToList();

            return View(specialProducts);
        }

    }
}
