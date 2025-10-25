using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Sweets.Models.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sweets.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        // ✅ اسم المنتج
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        // ✅ الرابط الفريد (Slug)
        [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug must be lowercase and can contain only letters, numbers, and hyphens")]
        public string? Slug { get; set; }

        // ✅ وصف المنتج
        [StringLength(500)]
        public string? Description { get; set; }

        // ✅ السعر
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 100000, ErrorMessage = "Price must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }


        // ✅ نسبة الخصم (اختيارية)
        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        [Display(Name = "Discount (%)")]
        public double? Discount { get; set; } = 0;

        // ✅ الصورة
        [ValidateNever]
        public List<ProductImage> ProductImages { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        // ✅ كمية المخزون
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a positive number")]
        public int StockQuantity { get; set; }

        // ✅ متاح أم لا
        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;

        // ✅ التقييم
        [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5")]
        public double Rating { get; set; } = 0;

       

        // ✅ التاريخ
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }
    }
}
