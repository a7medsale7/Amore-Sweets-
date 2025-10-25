using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sweet_Shop.Repository;
using Sweets.Models.Models;
using System.Security.Claims;

namespace Sweets_Shop_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class FeedbackController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public FeedbackController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ✅ عرض كل التعليقات (Approved فقط)
        public IActionResult Index()
        {
            var feedbacks = _unitOfWork.Feedback.GetAll(f => f.IsApproved)
                                                .OrderByDescending(f => f.CreatedAt)
                                                .ToList();
            return View(feedbacks);
        }

        // ✅ نموذج إضافة Feedback
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // ✅ حفظ Feedback جديد
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                feedback.CreatedAt = DateTime.UtcNow;
                feedback.IsApproved = false; // ✅ الأدمن هو اللي يوافق بعدين

                _unitOfWork.Feedback.Add(feedback);
                _unitOfWork.save();

                TempData["success"] = "Thank you for your feedback! It will be visible after admin approval.";
                return RedirectToAction("Index", "Home");
            }

            return View(feedback);
        }
    }
}
