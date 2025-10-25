using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Sweets.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }   // المفتاح الأساسي

        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(100)]
        public string Name { get; set; }   // اسم الكاتيجوري (مثلاً: كيك، دونات...)

        [MaxLength(250)]
        public string? Description { get; set; }  // وصف مختصر للقسم

        [Required]
        public bool IsActive { get; set; } = true;  // هل القسم متاح أو مخفي

        [Range(0, int.MaxValue)]
        [NotMapped]
        public int ItemCount => Products?.Count ?? 0;  // عدد المنتجات داخل القسم


        [StringLength(100)]
        public string? Slug { get; set; }  // اسم URL-friendly (مفيد للروابط / Cake) instead of "Category/1" use "Category/cakes"

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;  // وقت إنشاء القسم

        [DataType(DataType.DateTime)] 
        public DateTime? UpdatedDate { get; set; } = DateTime.Now; // آخر تعديل

       public List<Product> Products { get; set; } = new List<Product>();
        // قائمة المنتجات المرتبطة بهذا القسم
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }


    }
}
