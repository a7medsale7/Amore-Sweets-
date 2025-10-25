using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using Sweet_Shop.Repository;
using Sweets.Models.ViewData;
using Sweets.Utility;

namespace Sweets_Shop_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]

    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController (IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int id)
        {
            OrderVM orderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetails.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product")
            };

            if (orderVM.OrderHeader == null)
                return NotFound();

            return View(orderVM);
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.save();
            TempData["Success"] = "Order is now In Process.";
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }

        // ✅ Ship Order
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            if (orderHeader == null)
                return NotFound();

            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment && orderHeader.PaymentDueDate == null)
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);


            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.save();

            TempData["Success"] = "Order shipped successfully.";
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }
        // ✅ Pay Pending Order (if user wants to pay later)
        [HttpPost]
        [Authorize]
        [Area("Customer")]
        public IActionResult PayPendingOrder(int orderHeaderId)
        {
            var orderHeader = _unitOfWork.OrderHeader
                .Get(u => u.Id == orderHeaderId, includeProperties: "ApplicationUser");

            if (orderHeader == null)
                return NotFound();

            // نجيب تفاصيل الطلب الأصلي
            var orderDetails = _unitOfWork.OrderDetails
                .GetAll(u => u.OrderHeaderId == orderHeaderId, includeProperties: "Product");

            // نكوّن جلسة جديدة على Stripe
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderHeaderId}",
                CancelUrl = domain + $"customer/order/details?orderId={orderHeaderId}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in orderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
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
                };

                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            // نحفظ بيانات الـ Session الجديدة
            _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
            _unitOfWork.save();

            // نوجّه المستخدم مباشرة لصفحة الدفع على Stripe
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
        //-=====================

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader == null)
                return NotFound();

            var service = new SessionService();
            var session = service.Get(orderHeader.SessionId);

            // لو تم الدفع بنجاح
            if (session.PaymentStatus.ToLower() == "paid")
            {
                // تحديث بيانات الدفع
                _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);

                // تحديث الحالة إلى Approved فور الدفع
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, SD.StatusApproved, SD.PaymentStatusApproved);

                _unitOfWork.save();
            }

            // بعد الدفع نوجه المستخدم لصفحة تفاصيل الطلب
            return RedirectToAction("Details", "Order", new { area = "Admin", id = orderHeaderId });
        }



        // ✅ Cancel Order
        // ✅ Cancel Order (Safe & Complete Version)
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            if (orderHeader == null)
                return NotFound();

            try
            {
                // ✅ لو الدفع كان Approved نحاول نعمل Refund
                if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
                {
                    // لو الطلب تم دفعه فعلاً عبر Stripe (يعني PaymentIntentId مش فاضي)
                    if (!string.IsNullOrEmpty(orderHeader.PaymentIntentId))
                    {
                        var options = new RefundCreateOptions
                        {
                            Reason = RefundReasons.RequestedByCustomer,
                            PaymentIntent = orderHeader.PaymentIntentId
                        };

                        var service = new RefundService();
                        service.Create(options);

                        _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
                    }
                    else
                    {
                        // لو مفيش PaymentIntentId، نلغي بدون Refund
                        _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
                    }
                }
                else
                {
                    // ✅ في كل الحالات الأخرى (Pending, DelayedPayment...) نلغي الطلب عادي
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
                }

                _unitOfWork.save();
                TempData["Success"] = "Order cancelled successfully.";
            }
            catch (StripeException ex)
            {
                // لو حصل أي خطأ من Stripe أثناء الـ Refund
                TempData["Error"] = "Refund failed: " + ex.Message;
            }
            catch (Exception ex)
            {
                // أي خطأ غير متوقع
                TempData["Error"] = "An unexpected error occurred while cancelling the order: " + ex.Message;
            }

            // ✅ نرجع دايمًا لصفحة تفاصيل الطلب
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }


        // ✅ Pay Now (Stripe Checkout)
        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader
                .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetails = _unitOfWork.OrderDetails
                .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
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
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // ✅ Payment Confirmation
     
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeader == null)
                return NotFound();

            orderHeader.Name = OrderVM.OrderHeader.Name;
            orderHeader.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeader.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeader.City = OrderVM.OrderHeader.City;
            orderHeader.State = OrderVM.OrderHeader.State;
            orderHeader.PostalCode = OrderVM.OrderHeader.PostalCode;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.save();

            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { id = orderHeader.Id });
        }




        //API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            // نجيب كل الأوردرات مع المستخدم
            var orders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

            // نجيب هوية المستخدم الحالي
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // لو المستخدم مش Admin ولا Employee → نعرض بس طلباته هو
            if (!User.IsInRole(SD.Role_Admin) && !User.IsInRole(SD.Role_Employee))
            {
                orders = orders.Where(o => o.ApplicationUserId == userId).ToList();
            }

            // فلترة حسب الحالة (status)
            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                orders = orders.Where(o => o.OrderStatus == status).ToList();
            }

            // نرجّع البيانات
            return Json(new { data = orders });
        }



    }
}
