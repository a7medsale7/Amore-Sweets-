using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sweet_Shop.Repository;
using Sweets.Models;
using Sweets.Models.ViewData;
using Sweets.Models.Models;
using Sweets.Utility;

namespace Sweets_Shop_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            var products = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(products);
        }

        public IActionResult Details(string slug)
        {
            var product = _unitOfWork.Product.Get(p => p.Slug == slug, includeProperties: "Category,ProductImages");
            return View(product);
        }

        // ========== ADD ==========
        public IActionResult Add()
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                Product = new Product()
            };
            return View(productVM);
        }

        [HttpPost]
        public IActionResult Add(ProductVM productVM, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVM);
            }

            // Slug generation
            productVM.Product.Slug = productVM.Product.Name.ToLower().Replace(" ", "-");

            // check duplicate
            if (_unitOfWork.Product.Get(p => p.Name == productVM.Product.Name || p.Slug == productVM.Product.Slug) != null)
            {
                ModelState.AddModelError("", "Product name or slug already exists");
                return View(productVM);
            }

            // Save product first
            _unitOfWork.Product.Add(productVM.Product);
            _unitOfWork.save();

            // Handle multiple images
            string wwwRootPath = _hostEnvironment.WebRootPath;
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = @"images\products\product-" + productVM.Product.Id;
                    string finalPath = Path.Combine(wwwRootPath, productPath);

                    if (!Directory.Exists(finalPath))
                        Directory.CreateDirectory(finalPath);

                    using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    ProductImage productImage = new()
                    {
                        ImageUrl = @"\" + productPath + @"\" + fileName,
                        ProductId = productVM.Product.Id
                    };

                    if (productVM.Product.ProductImages == null)
                        productVM.Product.ProductImages = new List<ProductImage>();

                    productVM.Product.ProductImages.Add(productImage);
                }

                _unitOfWork.Product.Update(productVM.Product);
                _unitOfWork.save();
            }

            TempData["success"] = "Product created successfully";
            return RedirectToAction("Index");
        }

        // ========== EDIT ==========
        public IActionResult Edit(string slug)
        {
            var product = _unitOfWork.Product.Get(p => p.Slug == slug, includeProperties: "ProductImages");
            if (product == null)
                return NotFound();

            ProductVM productVM = new()
            {
                Product = product,
                CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM productVM, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
                return View(productVM);
            }

            productVM.Product.Slug = productVM.Product.Name.ToLower().Replace(" ", "-");

            var existingProduct = _unitOfWork.Product.Get(p =>
                (p.Name == productVM.Product.Name || p.Slug == productVM.Product.Slug) && p.Id != productVM.Product.Id);
            if (existingProduct != null)
            {
                ModelState.AddModelError("", "Product name or slug already exists");
                return View(productVM);
            }

            var productInDb = _unitOfWork.Product.Get(p => p.Id == productVM.Product.Id, includeProperties: "ProductImages");
            if (productInDb == null)
                return NotFound();

            string wwwRootPath = _hostEnvironment.WebRootPath;

            // handle multiple image upload
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = @"images\products\product-" + productInDb.Id;
                    string finalPath = Path.Combine(wwwRootPath, productPath);

                    if (!Directory.Exists(finalPath))
                        Directory.CreateDirectory(finalPath);

                    using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    ProductImage productImage = new()
                    {
                        ImageUrl = @"\" + productPath + @"\" + fileName,
                        ProductId = productInDb.Id
                    };

                    _unitOfWork.ProductImage.Add(productImage);
                }

                _unitOfWork.save();
            }

            // update fields
            productInDb.Name = productVM.Product.Name;
            productInDb.Description = productVM.Product.Description;
            productInDb.Price = productVM.Product.Price;
            productInDb.Discount = productVM.Product.Discount;
            productInDb.StockQuantity = productVM.Product.StockQuantity;
            productInDb.CategoryId = productVM.Product.CategoryId;
            productInDb.IsAvailable = productVM.Product.IsAvailable;
            productInDb.Rating = productVM.Product.Rating;
            productInDb.Slug = productVM.Product.Slug;
            productInDb.UpdatedAt = DateTime.Now;

            _unitOfWork.Product.Update(productInDb);
            _unitOfWork.save();

            TempData["success"] = "Product updated successfully";
            return RedirectToAction("Index");
        }

        // ========== DELETE IMAGE ==========
        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            if (imageToBeDeleted == null)
                return NotFound();

            int productId = imageToBeDeleted.ProductId;

            if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
            {
                var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.ProductImage.Remove(imageToBeDeleted);
            _unitOfWork.save();

            TempData["success"] = "Image deleted successfully";
            return RedirectToAction(nameof(Edit), new { slug = _unitOfWork.Product.Get(p => p.Id == productId).Slug });
        }

        // ========== DELETE PRODUCT ==========
        [HttpPost]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "ProductImages");
            if (obj == null)
                return Json(new { success = false, message = "Error while deleting" });

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_hostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                    System.IO.File.Delete(filePath);

                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Remove(obj);
            _unitOfWork.save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        [HttpPost]
        public IActionResult ToggleSpecial(int id)
        {
            if (Sweets.Utility.SpecialProductsCache.IsSpecial(id))
                Sweets.Utility.SpecialProductsCache.Remove(id);
            else
                Sweets.Utility.SpecialProductsCache.Add(id);

            TempData["success"] = "Special products updated successfully!";
            return RedirectToAction(nameof(Index));
        }


        // ========== API ==========
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages").ToList();
            return Json(new { data = objProductList });
        }

    }
}
