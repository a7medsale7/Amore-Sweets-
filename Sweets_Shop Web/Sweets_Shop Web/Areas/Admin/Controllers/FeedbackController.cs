using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sweet_Shop.Repository;
using Sweets.Models.Models;
using Sweets.Utility;

namespace Sweets_Shop_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class FeedbackController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public FeedbackController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ✅ عرض كل التعليقات
        public IActionResult Index()
        {
            var feedbacks = _unitOfWork.Feedback.GetAll()
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
            return View(feedbacks);
        }

        // ✅ الموافقة على تعليق
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            var feedback = _unitOfWork.Feedback.Get(f => f.Id == id);
            if (feedback == null)
                return NotFound();

            feedback.IsApproved = true;
            _unitOfWork.Feedback.Update(feedback);
            _unitOfWork.save();

            TempData["success"] = "Feedback approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ حذف تعليق
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var feedback = _unitOfWork.Feedback.Get(f => f.Id == id);
            if (feedback == null)
                return NotFound();

            _unitOfWork.Feedback.Remove(feedback);
            _unitOfWork.save();

            TempData["success"] = "Feedback deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
