using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sweet_Shop.Repository;
using Sweets.Models;
using Sweets.Utility;

namespace Sweets_Shop_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        // ✅ عرض كل الأقسام
        public IActionResult Index()
        {
            var categories = _unitOfWork.Category.GetAll(includeProperties: "Products").ToList();
            return View(categories);
        }

        // ✅ عرض تفاصيل القسم
        public IActionResult Details(string slug)
        {
            var category = _unitOfWork.Category.Get(c => c.Slug == slug, includeProperties: "Products");
            if (category == null)
                return NotFound();

            return View(category);
        }

        // ✅ عرض صفحة الإضافة
        public IActionResult Add()
        {
            return View();
        }

        // ✅ تنفيذ الإضافة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            // إنشاء slug من الاسم
            category.Slug = category.Name.ToLower().Replace(" ", "-");

            // التحقق من التكرار
            if (_unitOfWork.Category.Get(c => c.Name == category.Name || c.Slug == category.Slug) != null)
            {
                ModelState.AddModelError("", "Category name or slug already exists");
                return View(category);
            }

            // ✅ رفع الصورة
            if (category.ImageFile != null)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(category.ImageFile.FileName);
                string categoryPath = Path.Combine(wwwRootPath, "images", "categories");

                if (!Directory.Exists(categoryPath))
                    Directory.CreateDirectory(categoryPath);

                using (var fileStream = new FileStream(Path.Combine(categoryPath, fileName), FileMode.Create))
                {
                    category.ImageFile.CopyTo(fileStream);
                }

                category.ImageUrl = "/images/categories/" + fileName;
            }

            _unitOfWork.Category.Add(category);
            _unitOfWork.save();

            TempData["success"] = "Category created successfully";
            return RedirectToAction("Index");
        }

        // ✅ عرض صفحة التعديل
        public IActionResult Edit(string slug)
        {
            if (slug == null) return NotFound();

            var category = _unitOfWork.Category.Get(c => c.Slug == slug, tracked: true);
            if (category == null) return NotFound();

            return View(category);
        }

        // ✅ تنفيذ التعديل
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            category.Slug = category.Name.ToLower().Replace(" ", "-");

            // التحقق من التكرار
            var exists = _unitOfWork.Category.Get(c =>
                (c.Name == category.Name || c.Slug == category.Slug) && c.Id != category.Id);

            if (exists != null)
            {
                ModelState.AddModelError("", "Category name or slug already exists");
                return View(category);
            }

            // ✅ تحديث الصورة
            if (category.ImageFile != null)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(category.ImageFile.FileName);
                string categoryPath = Path.Combine(wwwRootPath, "images", "categories");

                if (!Directory.Exists(categoryPath))
                    Directory.CreateDirectory(categoryPath);

                // حذف الصورة القديمة لو موجودة
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    var oldImagePath = Path.Combine(wwwRootPath, category.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                using (var fileStream = new FileStream(Path.Combine(categoryPath, fileName), FileMode.Create))
                {
                    category.ImageFile.CopyTo(fileStream);
                }

                category.ImageUrl = "/images/categories/" + fileName;
            }

            _unitOfWork.Category.Update(category);
            _unitOfWork.save();

            TempData["success"] = "Category updated successfully";
            return RedirectToAction("Index");
        }

        // ✅ حذف القسم
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                var category = _unitOfWork.Category.Get(c => c.Id == id);
                if (category == null)
                    return Json(new { success = false, message = "Category not found." });

                // ✅ حذف الصورة لو موجودة
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                _unitOfWork.Category.Remove(category);
                _unitOfWork.save();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // 🔥 هنا نرجع الخطأ عشان نشوف السبب الحقيقي
                return Json(new { success = false, message = ex.Message });
            }
        }


        // ✅ إرجاع كل الكاتيجوريز كـ JSON
        [HttpGet]
        public IActionResult GetAll()
        {
            var categories = _unitOfWork.Category.GetAll(includeProperties: "Products").ToList();
            return Json(new { data = categories });
        }
    }
}
