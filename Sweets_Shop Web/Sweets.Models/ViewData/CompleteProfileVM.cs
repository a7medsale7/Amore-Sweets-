using Sweets.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweets.Models.ViewData
{
    public class CompleteProfileVM
    {
        public ApplicationUser ApplicationUser { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
