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

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
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

            // جلب بيانات المستخدم وتعبئة الـ OrderHeader لعرضها في النموذج (View)
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(c => c.Id == userId);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            // حساب المجموع الكلي
            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                ShoppingCartVM.OrderHeader.OrderTotal += (double)item.Product.Price * item.Count;
            }
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // 1. استرجاع قائمة التسوق
            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                includeProperties: "Product"
            );

            // جلب بيانات المستخدم
            var appUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            // 2. ✅ الحل: تعبئة حقول العنوان المفقودة أو الفارغة من بيانات المستخدم
            // (لضمان أن الحقول الإلزامية مثل City ليست NULL)
            if (string.IsNullOrEmpty(ShoppingCartVM.OrderHeader.Name))
            {
                ShoppingCartVM.OrderHeader.Name = appUser.Name;
            }
            if (string.IsNullOrEmpty(ShoppingCartVM.OrderHeader.PhoneNumber))
            {
                ShoppingCartVM.OrderHeader.PhoneNumber = appUser.PhoneNumber;
            }
            if (string.IsNullOrEmpty(ShoppingCartVM.OrderHeader.StreetAddress))
            {
                ShoppingCartVM.OrderHeader.StreetAddress = appUser.StreetAddress;
            }
            if (string.IsNullOrEmpty(ShoppingCartVM.OrderHeader.City))
            {
                // هذا هو الكود الذي يحل مشكلة الـ 'City'
                ShoppingCartVM.OrderHeader.City = appUser.City;
            }
            if (string.IsNullOrEmpty(ShoppingCartVM.OrderHeader.State))
            {
                ShoppingCartVM.OrderHeader.State = appUser.State;
            }
            if (string.IsNullOrEmpty(ShoppingCartVM.OrderHeader.PostalCode))
            {
                ShoppingCartVM.OrderHeader.PostalCode = appUser.PostalCode;
            }


            // 3. إعداد الـ OrderHeader
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;

            // 4. حساب الإجمالي النهائي
            double total = 0;
            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                item.Price = (double)item.Product.Price;
                total += item.Price * item.Count;
            }
            ShoppingCartVM.OrderHeader.OrderTotal = total;

            // 5. حفظ الـ OrderHeader في قاعدة البيانات
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.save();

            // 6. حفظ تفاصيل الأوردر (OrderDetails)
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


            // ==========================
            // Stripe Session Creation (بدء عملية الدفع)
            // ==========================
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
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        // يجب ضرب السعر في 100 لأنه يتم التعامل بالـ Cents
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            // حفظ الـ Stripe IDs
            _unitOfWork.OrderHeader.UpdateStripePaymentID(
                ShoppingCartVM.OrderHeader.Id,
                session.Id,
                session.PaymentIntentId
            );
            _unitOfWork.save();

            // التوجيه لصفحة الدفع
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