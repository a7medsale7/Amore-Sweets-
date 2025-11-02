using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Sweet_Shop.Repository;
using Sweets.Models.Models;
using Sweets.Models.ViewData;
using Sweets.Utility;
using System.Security.Claims;

namespace Sweets_Shop_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]

    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
                    c => c.ApplicationUserId == userId,
                    includeProperties: "Product,Product.ProductImages"
                ),
                OrderHeader = new OrderHeader()
            };

            double orderTotal = 0;

            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                item.Price = (double)item.Product.Price;
                orderTotal += item.Price * item.Count;
            }

            ShoppingCartVM.OrderHeader.OrderTotal = orderTotal;
            var appUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
            if (appUser != null)
            {
                ShoppingCartVM.OrderHeader.ApplicationUser = appUser;
                ShoppingCartVM.OrderHeader.ApplicationUserId = appUser.Id;
                ShoppingCartVM.OrderHeader.Name = appUser.Name ?? string.Empty;
                ShoppingCartVM.OrderHeader.PhoneNumber = appUser.PhoneNumber ?? string.Empty;
                ShoppingCartVM.OrderHeader.StreetAddress = appUser.StreetAddress ?? string.Empty;
                ShoppingCartVM.OrderHeader.City = appUser.City ?? string.Empty;
                ShoppingCartVM.OrderHeader.State = appUser.State ?? string.Empty;
                ShoppingCartVM.OrderHeader.PostalCode = appUser.PostalCode ?? string.Empty;
            }

            return View(ShoppingCartVM);
        }

        [Authorize]
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
                    c => c.ApplicationUserId == userId,
                    includeProperties: "Product"
                ),
                OrderHeader = new()
                {
                    OrderTotal = 0
                }
            };

            var appUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            if (appUser != null)
            {
                ShoppingCartVM.OrderHeader.ApplicationUser = appUser;
                ShoppingCartVM.OrderHeader.ApplicationUserId = appUser.Id;
                ShoppingCartVM.OrderHeader.Name = appUser.Name ?? string.Empty;
                ShoppingCartVM.OrderHeader.PhoneNumber = appUser.PhoneNumber ?? string.Empty;
                ShoppingCartVM.OrderHeader.StreetAddress = appUser.StreetAddress ?? string.Empty;
                ShoppingCartVM.OrderHeader.City = appUser.City ?? string.Empty;
                ShoppingCartVM.OrderHeader.State = appUser.State ?? string.Empty;
                ShoppingCartVM.OrderHeader.PostalCode = appUser.PostalCode ?? string.Empty;
            }
            else
            {
                return RedirectToAction("CompleteProfile", "Account", new
                {
                    area = "Customer",
                    returnUrl = Url.Action("Summary", "Cart", new { area = "Customer" })
                });
            }

            // 💰 حساب السعر الكامل لكل منتج والمجموع النهائي
            double total = 0;
            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                item.Price = (double)item.Product.Price; // تأكيد أن السعر يجي من المنتج
                total += item.Price * item.Count;
            }

            ShoppingCartVM.OrderHeader.OrderTotal = total;

            return View(ShoppingCartVM);
        }


        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                // لو السيشن انتهت أو المستخدم مش معروف
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var userId = claim.Value;

            // 🟢 نحاول نجيب المستخدم من جدول ApplicationUser
            var appUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            if (appUser == null)
            {
                // 🟥 المستخدم داخل بجوجل ومش موجود في جدول ApplicationUser
                return RedirectToAction("CompleteProfile", "Account", new
                {
                    area = "Customer",
                    returnUrl = Url.Action("Summary", "Cart", new { area = "Customer" })
                });
            }

            // ✅ تحقق من وجود بيانات المستخدم الأساسية
            if (string.IsNullOrEmpty(appUser.City) ||
                string.IsNullOrEmpty(appUser.StreetAddress) ||
                string.IsNullOrEmpty(appUser.Name))
            {
                // 🔁 تحويل المستخدم لصفحة استكمال البيانات
                return RedirectToAction("CompleteProfile", "Account", new
                {
                    area = "Customer",
                    returnUrl = Url.Action("Summary", "Cart", new { area = "Customer" })
                });
            }

            // 🛒 استرجاع السلة
            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                includeProperties: "Product"
            );

            // 🔧 إعداد بيانات الطلب
            ShoppingCartVM.OrderHeader = new OrderHeader
            {
                ApplicationUserId = userId,
                Name = appUser.Name,
                PhoneNumber = appUser.PhoneNumber,
                StreetAddress = appUser.StreetAddress,
                City = appUser.City,
                State = appUser.State,
                PostalCode = appUser.PostalCode,
                OrderDate = DateTime.Now,
                OrderStatus = SD.StatusPending,
                PaymentStatus = SD.PaymentStatusPending
            };

            // 💰 حساب الإجمالي
            double total = 0;
            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                item.Price = (double)item.Product.Price;
                total += item.Price * item.Count;
            }
            ShoppingCartVM.OrderHeader.OrderTotal = total;

            // 🧾 حفظ الأوردر
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.save();

            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetails orderDetails = new()
                {
                    ProductId = item.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };
                _unitOfWork.OrderDetails.Add(orderDetails);
            }
            _unitOfWork.save();

            // 💳 إعداد جلسة الدفع في Stripe
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                CancelUrl = domain + "customer/cart/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name
                        }
                    },
                    Quantity = item.Count
                });
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStripePaymentID(
                ShoppingCartVM.OrderHeader.Id,
                session.Id,
                session.PaymentIntentId
            );
            _unitOfWork.save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }



        public IActionResult OrderConfirmation(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                // تحديث حالة الطلب والدفع
                _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.save();

                // امسح السلة بعد الدفع الناجح
                var carts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId);
                _unitOfWork.ShoppingCart.RemoveRange(carts);
                _unitOfWork.save();
            }

            return View(id);
        }


        public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
            cart.Count += 1;
            _unitOfWork.ShoppingCart.Update(cart);
            _unitOfWork.save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
            if (cart.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(cart);
            }
            else
            {
                cart.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cart);
            }
            _unitOfWork.save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.save();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // حدّث الكاونتر بعد الحذف
            HttpContext.Session.SetInt32(SD.SessionCart,
                _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId).Count());
            TempData["success"] = "Item removed from cart";
            return RedirectToAction(nameof(Index));
        }


    }
}