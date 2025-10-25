using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweets.Models.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public string Name { get; set; }          // اسم المرسل
        public string Comment { get; set; }       // نص التعليق
        public int Rating { get; set; } = 5;      // تقييم (1-5) اختياري
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = false; // 
    }
}
