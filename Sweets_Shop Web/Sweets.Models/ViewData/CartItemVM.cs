using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweets.Models.ViewData
{
    public class CartItemVM
    {
        public int Id { get; set; }
        public Product Product { get; set; }
        public int Count { get; set; }
        public double Price { get; set; } // مهم: السعر الحالي للمنتج
        public int ProductId { get; set; } // لتسهيل حفظ O
    }
}
